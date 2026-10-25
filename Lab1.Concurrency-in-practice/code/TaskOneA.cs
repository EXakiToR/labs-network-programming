using System;
using System.IO;
using System.Threading;


class Program
{
    static int Main(string[] args)
    {
        // accept only the number of threads. 
        // file is loceted in "./numbers.txt", yes, hardcoded intensionally

        if (args.Length != 1 ||
            !int.TryParse(args[0], out int numberOfThreads) ||
            numberOfThreads <= 0)
        {
            Console.Error.WriteLine("Error: Expected one positive integer argument.");
            return 1;
        }


        const string filePath = "numbers.txt";
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Error: File '{filePath}' was not found.");
            return 1;
        }

        long[] negativeCounts = new long[numberOfThreads];
        long[] positiveCounts = new long[numberOfThreads];
        long[] zeroCounts = new long[numberOfThreads];
        Exception?[] errors = new Exception?[numberOfThreads];
        Thread[] workers = new Thread[numberOfThreads];

        long fileLength = new FileInfo(filePath).Length;
        for (int i = 0; i < numberOfThreads; i++)
        {
            int workerIndex = i;
            long start = fileLength * workerIndex / numberOfThreads;
            long end = fileLength * (workerIndex + 1) / numberOfThreads;

            workers[i] = new Thread(() =>
            {
                try
                {
                    CountPart(
                        filePath,
                        start,
                        end,
                        fileLength,
                        out negativeCounts[workerIndex],
                        out positiveCounts[workerIndex],
                        out zeroCounts[workerIndex]);
                }
                catch (Exception exception)
                {
                    errors[workerIndex] = exception;
                }
            });
            workers[i].Start();
        }

        foreach (Thread worker in workers)
        {
            worker.Join();
        }

        for (int i = 0; i < errors.Length; i++)
        {
            if (errors[i] is not null)
            {
                Console.Error.WriteLine($"Error while reading part {i + 1}: {errors[i]!.Message}");
                return 1;
            }
        }

        long negative = 0;
        long positive = 0;
        long zero = 0;
        for (int i = 0; i < numberOfThreads; i++)
        {
            negative += negativeCounts[i];
            positive += positiveCounts[i];
            zero += zeroCounts[i];
        }

        Console.WriteLine($"Negative: {negative}");
        Console.WriteLine($"Positive: {positive}");
        Console.WriteLine($"Zero: {zero}");
        return 0;
    }

    private static void CountPart(
        string filePath,
        long start,
        long end,
        long fileLength,
        out long negative,
        out long positive,
        out long zero)
    {
        negative = 0;
        positive = 0;
        zero = 0;

        using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        long position = start;

        if (start > 0)
        {
            stream.Position = start - 1;
            int previousByte = stream.ReadByte();
            stream.Position = start;

            if (previousByte != '\n')
            {
                using BufferedFileReader reader = new BufferedFileReader(stream);
                SkipToNextLine(reader, ref position, fileLength);
                CountRemainingLines(reader, ref position, end, fileLength, ref negative, ref positive, ref zero);
                return;
            }
        }

        using BufferedFileReader bufferedReader = new BufferedFileReader(stream);
        CountRemainingLines(bufferedReader, ref position, end, fileLength, ref negative, ref positive, ref zero);
    }

    private static void CountRemainingLines(
        BufferedFileReader reader,
        ref long position,
        long end,
        long fileLength,
        ref long negative,
        ref long positive,
        ref long zero)
    {
        while (position < end)
        {
            int value = ReadNumber(reader, ref position, fileLength);
            if (value < 0)
            {
                negative++;
            }
            else if (value > 0)
            {
                positive++;
            }
            else
            {
                zero++;
            }
        }
    }

    private static void SkipToNextLine(BufferedFileReader reader, ref long position, long fileLength)
    {
        while (position < fileLength)
        {
            if (reader.ReadByte() == '\n')
            {
                position++;
                return;
            }

            position++;
        }
    }

    private static int ReadNumber(BufferedFileReader reader, ref long position, long fileLength)
    {
        int sign = 1;
        long value = 0;
        bool hasDigits = false;
        bool hasSign = false;

        while (position < fileLength)
        {
            int currentByte = reader.ReadByte();
            position++;

            if (currentByte == '\n')
            {
                break;
            }

            if (currentByte == '\r' || currentByte == ' ' || currentByte == '\t')
            {
                continue;
            }

            if ((currentByte == '-' || currentByte == '+') && !hasSign && !hasDigits)
            {
                sign = currentByte == '-' ? -1 : 1;
                hasSign = true;
                continue;
            }

            if (currentByte < '0' || currentByte > '9')
            {
                throw new FormatException($"Invalid character '{(char)currentByte}' in numbers.txt.");
            }

            hasDigits = true;
            value = checked(value * 10 + (currentByte - '0'));
        }

        if (!hasDigits)
        {
            throw new FormatException("A line in numbers.txt does not contain an integer.");
        }

        return checked((int)(sign * value));
    }

    private sealed class BufferedFileReader : IDisposable
    {
        private readonly FileStream _stream;
        private readonly byte[] _buffer = new byte[64 * 1024];
        private int _bufferPosition;
        private int _bufferLength;

        public BufferedFileReader(FileStream stream)
        {
            _stream = stream;
        }

        public int ReadByte()
        {
            if (_bufferPosition >= _bufferLength)
            {
                _bufferLength = _stream.Read(_buffer, 0, _buffer.Length);
                _bufferPosition = 0;
                if (_bufferLength == 0)
                {
                    return -1;
                }
            }

            return _buffer[_bufferPosition++];
        }

        public void Dispose()
        {
        }
    }
}