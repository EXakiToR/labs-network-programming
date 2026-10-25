using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;


class Program
{
    private const int BatchSize = 10_000;
    private const int QueueCapacity = 100;

    static int Main(string[] args)
    {
        if (args.Length != 1 ||
            !int.TryParse(args[0], out int workerCount) ||
            workerCount <= 0)
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

        using BlockingCollection<List<string>> queue = new BlockingCollection<List<string>>(QueueCapacity);
        using CancellationTokenSource cancellation = new CancellationTokenSource();

        long[] negativeCounts = new long[workerCount];
        long[] positiveCounts = new long[workerCount];
        long[] zeroCounts = new long[workerCount];
        Exception? producerError = null;
        Exception?[] workerErrors = new Exception?[workerCount];

        Thread producer = new Thread(() =>
        {
            try
            {
                ProduceBatches(filePath, queue, cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                producerError = exception;
                cancellation.Cancel();
            }
            finally
            {
                queue.CompleteAdding();
            }
        });

        Thread[] workers = new Thread[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            int workerIndex = i;
            workers[i] = new Thread(() =>
            {
                try
                {
                    ConsumeBatches(
                        queue,
                        cancellation.Token,
                        out negativeCounts[workerIndex],
                        out positiveCounts[workerIndex],
                        out zeroCounts[workerIndex]);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    workerErrors[workerIndex] = exception;
                    cancellation.Cancel();
                }
            });
        }

        producer.Start();
        foreach (Thread worker in workers)
        {
            worker.Start();
        }

        producer.Join();
        foreach (Thread worker in workers)
        {
            worker.Join();
        }

        if (producerError is not null)
        {
            Console.Error.WriteLine($"Error while reading numbers.txt: {producerError.Message}");
            return 1;
        }

        for (int i = 0; i < workerErrors.Length; i++)
        {
            if (workerErrors[i] is not null)
            {
                Console.Error.WriteLine($"Error in worker {i + 1}: {workerErrors[i]!.Message}");
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

    private static void ProduceBatches(
        string filePath,
        BlockingCollection<List<string>> queue,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = new StreamReader(filePath);
        List<string> batch = new List<string>(BatchSize);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            batch.Add(line);

            if (batch.Count == BatchSize)
            {
                queue.Add(batch, cancellationToken);
                batch = new List<string>(BatchSize);
            }
        }

        if (batch.Count > 0)
        {
            queue.Add(batch, cancellationToken);
        }
    }

    private static void ConsumeBatches(
        BlockingCollection<List<string>> queue,
        CancellationToken cancellationToken,
        out long negative,
        out long positive,
        out long zero)
    {
        negative = 0;
        positive = 0;
        zero = 0;

        foreach (List<string> batch in queue.GetConsumingEnumerable(cancellationToken))
        {
            foreach (string line in batch)
            {
                if (!int.TryParse(line, out int value))
                {
                    throw new FormatException($"Invalid integer '{line}'.");
                }

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
    }
}
