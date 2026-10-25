using System;
using System.IO;
using System.Threading;


class Program
{
    private const int FileCount = 8;

    static int Main(string[] args)
    {
        if ((args.Length != 1 && args.Length != 2) ||
            !int.TryParse(args[0], out int workerCount) ||
            workerCount <= 0)
        {
            Console.Error.WriteLine("Usage: dotnet run TaskTwo.cs <workers> [directory]");
            return 1;
        }

        string directory = args.Length == 2 ? args[1] : Directory.GetCurrentDirectory();
        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"Error: Directory '{directory}' was not found.");
            return 1;
        }

        string[] filePaths = new string[FileCount];
        for (int i = 0; i < FileCount; i++)
        {
            filePaths[i] = Path.Combine(directory, $"numbers{i + 1}.txt");
            if (!File.Exists(filePaths[i]))
            {
                Console.Error.WriteLine($"Error: File '{filePaths[i]}' was not found.");
                return 1;
            }
        }

        long[] negativeCounts = new long[workerCount];
        long[] positiveCounts = new long[workerCount];
        long[] zeroCounts = new long[workerCount];
        Exception?[] errors = new Exception?[workerCount];
        Thread[] workers = new Thread[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            int workerIndex = i;
            int firstFile = FileCount * workerIndex / workerCount;
            int lastFile = FileCount * (workerIndex + 1) / workerCount;

            workers[i] = new Thread(() =>
            {
                try
                {
                    CountFiles(
                        filePaths,
                        firstFile,
                        lastFile,
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
                Console.Error.WriteLine($"Error in worker {i + 1}: {errors[i]!.Message}");
                return 1;
            }
        }

        long negative = 0;
        long positive = 0;
        long zero = 0;
        for (int i = 0; i < workerCount; i++)
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

    private static void CountFiles(
        string[] filePaths,
        int firstFile,
        int lastFile,
        out long negative,
        out long positive,
        out long zero)
    {
        negative = 0;
        positive = 0;
        zero = 0;

        for (int fileIndex = firstFile; fileIndex < lastFile; fileIndex++)
        {
            byte[] bytes = File.ReadAllBytes(filePaths[fileIndex]);
            CountNumbers(bytes, ref negative, ref positive, ref zero);
        }
    }

    private static void CountNumbers(
        byte[] bytes,
        ref long negative,
        ref long positive,
        ref long zero)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            while (index < bytes.Length &&
                   (bytes[index] == (byte)'\r' ||
                    bytes[index] == (byte)'\n' ||
                    bytes[index] == (byte)' ' ||
                    bytes[index] == (byte)'\t'))
            {
                index++;
            }

            if (index >= bytes.Length)
            {
                break;
            }

            int sign = 1;
            if (bytes[index] == (byte)'-' || bytes[index] == (byte)'+')
            {
                if (bytes[index] == (byte)'-')
                {
                    sign = -1;
                }

                index++;
            }

            if (index >= bytes.Length || bytes[index] < (byte)'0' || bytes[index] > (byte)'9')
            {
                throw new FormatException("An input file contains a non-integer value.");
            }

            long value = 0;
            while (index < bytes.Length && bytes[index] >= (byte)'0' && bytes[index] <= (byte)'9')
            {
                value = checked(value * 10 + bytes[index] - (byte)'0');
                index++;
            }

            int signedValue = checked((int)(sign * value));
            if (signedValue < 0)
            {
                negative++;
            }
            else if (signedValue > 0)
            {
                positive++;
            }
            else
            {
                zero++;
            }

            if (index < bytes.Length &&
                bytes[index] != (byte)'\r' &&
                bytes[index] != (byte)'\n' &&
                bytes[index] != (byte)' ' &&
                bytes[index] != (byte)'\t')
            {
                throw new FormatException("An input file contains invalid characters after an integer.");
            }
        }
    }
}
