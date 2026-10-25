#:include ../../code/TaskOneB.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
class Benchmark
{
    static int Main(string[] args)
    {
        if (args.Length != 3 || !int.TryParse(args[1], out int workers) || workers <= 0)
        {
            Console.Error.WriteLine("Usage: TaskOneBBenchmark <version> <workers> <numbers.txt>");
            return 1;
        }
        int[] numbers=File.ReadLines(args[2]).Select(int.Parse).ToArray();
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        Func<int[],int,SignCounts> algorithm=args[0] switch {
            "Unsynchronized" => TaskOneB.CountUnsynchronized,
            "Locked" => TaskOneB.CountWithLock,
            "LocalCounters" => TaskOneB.CountWithLocalCounters,
            _ => throw new ArgumentException("Unknown version")
        };
        var clock=Stopwatch.StartNew();
        var counts=algorithm(numbers,workers);
        clock.Stop();
        Console.WriteLine("ELAPSED_MS=" + clock.Elapsed.TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
        Console.WriteLine($"Negative: {counts.Negative}");
        Console.WriteLine($"Positive: {counts.Positive}");
        Console.WriteLine($"Zero: {counts.Zero}");
        Console.WriteLine($"LOGICAL_PROCESSORS={Environment.ProcessorCount}");
        return 0;
    }
}
