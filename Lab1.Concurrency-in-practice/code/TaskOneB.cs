
using System;
using System.Threading;

public record SignCounts(long Negative, long Positive, long Zero);

public static class TaskOneB
{
    // 1. Shared counters without synchronization
    public static SignCounts CountUnsynchronized(int[] numbers, int workers)
    {
        long negative = 0, positive = 0, zero = 0;

        RunWorkers(numbers.Length, workers, (start, end) =>
        {
            for (int i = start; i < end; i++)
            {
                if (numbers[i] < 0) negative++;
                else if (numbers[i] > 0) positive++;
                else zero++;
            }
        });

        return new SignCounts(negative, positive, zero);
    }

    // 2. Shared counters protected by lock
    public static SignCounts CountWithLock(int[] numbers, int workers)
    {
        long negative = 0, positive = 0, zero = 0;
        object sync = new();

        RunWorkers(numbers.Length, workers, (start, end) =>
        {
            for (int i = start; i < end; i++)
            {
                lock (sync)
                {
                    if (numbers[i] < 0) negative++;
                    else if (numbers[i] > 0) positive++;
                    else zero++;
                }
            }
        });

        return new SignCounts(negative, positive, zero);
    }

    // 3. One set of counters per worker
    public static SignCounts CountWithLocalCounters(int[] numbers, int workers)
    {
        SignCounts[] results = new SignCounts[workers];

        RunWorkers(numbers.Length, workers, (start, end, workerId) =>
        {
            long negative = 0, positive = 0, zero = 0;

            for (int i = start; i < end; i++)
            {
                if (numbers[i] < 0) negative++;
                else if (numbers[i] > 0) positive++;
                else zero++;
            }

            results[workerId] = new SignCounts(negative, positive, zero);
        });

        long totalNegative = 0, totalPositive = 0, totalZero = 0;

        foreach (var result in results)
        {
            totalNegative += result.Negative;
            totalPositive += result.Positive;
            totalZero += result.Zero;
        }

        return new SignCounts(totalNegative, totalPositive, totalZero);
    }

    private static void RunWorkers(
        int length, int workers, Action<int, int> action)
    {
        RunWorkers(length, workers,
            (start, end, id) => action(start, end));
    }

    private static void RunWorkers(
        int length, int workers, Action<int, int, int> action)
    {
        Thread[] threads = new Thread[workers];

        for (int id = 0; id < workers; id++)
        {
            int workerId = id;
            int start = (int)((long)length * id / workers);
            int end = (int)((long)length * (id + 1) / workers);

            threads[id] = new Thread(() =>
                action(start, end, workerId));
        }

        foreach (var thread in threads)
            thread.Start();

        foreach (var thread in threads)
            thread.Join();
    }
}
