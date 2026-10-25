using System;
using System.Diagnostics;

public record MetricTaskOneA(
    int Threads,
    long ElapsedMs,
    long peakMemoryBytes
);

public record MetricTaskOneC(
    int Workers,
    long ElapsedMs,
    long peakMemoryBytes
);


public class MetriccolelctorTaskOneA
{
    // should measure the time and peak memory. 
    private Stopwatch? _stopwatch;
    private long _peakMemoryBytes;

    //private Process _process;
    List<MetricTaskOneA> metrics = new();
    public MetriccolelctorTaskOneA()
    {

    }

    public void Collect(Process process, int numberOfThreads)
    {
        _stopwatch = Stopwatch.StartNew();
        _peakMemoryBytes = 0;
    }
    public void StopCollect(int numberOfThreads, long peakMemoryBytes)
    {
        if (_stopwatch is null)
            throw new InvalidOperationException();

        _stopwatch.Stop();

        long elapsedMs = _stopwatch.ElapsedMilliseconds;

        metricSave(numberOfThreads, elapsedMs, peakMemoryBytes);
    }

    private void metricSave(int numberOfThreads, long elapsedMs, long peakMemoryBytes)
    {
        metrics.Add(new MetricTaskOneA(numberOfThreads, elapsedMs, peakMemoryBytes));
    }

    public List<MetricTaskOneA> ExportMetrics()
    {
        return metrics;
    }

    public void TrackMemoryUntilExit(Process process)
    {
        while (!process.WaitForExit(5))
        {
            try
            {
                process.Refresh();

                _peakMemoryBytes = Math.Max(
                    _peakMemoryBytes,
                    process.PeakWorkingSet64
                );
            }
            catch (InvalidOperationException)
            {
                break; // Process has already exited
            }
        }
    }

}

public class MetriccolelctorTaskOneC
{
    // should measure the time and peak memory. 
    private Stopwatch? _stopwatch;
    private long _peakMemoryBytes;

    //private Process _process;
    List<MetricTaskOneA> metrics = new();
    public MetriccolelctorTaskOneC()
    {

    }

    public void Collect(Process process, int numberOfThreads)
    {
        _stopwatch = Stopwatch.StartNew();
        _peakMemoryBytes = 0;
    }
    public void StopCollect(int numberOfThreads, long peakMemoryBytes)
    {
        if (_stopwatch is null)
            throw new InvalidOperationException();

        _stopwatch.Stop();

        long elapsedMs = _stopwatch.ElapsedMilliseconds;

        metricSave(numberOfThreads, elapsedMs, peakMemoryBytes);
    }

    private void metricSave(int numberOfThreads, long elapsedMs, long peakMemoryBytes)
    {
        metrics.Add(new MetricTaskOneA(numberOfThreads, elapsedMs, peakMemoryBytes));
    }

    public List<MetricTaskOneA> ExportMetrics()
    {
        return metrics;
    }

    public void TrackMemoryUntilExit(Process process)
    {
        while (!process.WaitForExit(5))
        {
            try
            {
                process.Refresh();

                _peakMemoryBytes = Math.Max(
                    _peakMemoryBytes,
                    process.PeakWorkingSet64
                );
            }
            catch (InvalidOperationException)
            {
                break; // Process has already exited
            }
        }
    }

}

public class Application
{
    private readonly MetriccolelctorTaskOneA _metricCollectorTaskOneA;
    private readonly MetriccolelctorTaskOneC _metricCollectorTaskOneC;

    public Application(MetriccolelctorTaskOneA metricCollectorTaskOneA, MetriccolelctorTaskOneC metricCollectorTaskOneC)
    {
        _metricCollectorTaskOneA = metricCollectorTaskOneA;
        _metricCollectorTaskOneC = metricCollectorTaskOneC;
    }

    // use cases

    public void GenerateFileUseCase()
    {
        Console.WriteLine("Choosed the generation of a input file.");
        // Generate a text file numbers.txt containing 50000000 random integers from −1000000 to 1000000, one number per line.
        // For Task 2, split numbers.txt, in order, into 8 files of 6250000 numbers each.

        // we will create numbers.txt and numbers1.txt, numbers2.txt etc..

        // path: [./numbers.txt], in the same folter with Program.cs


        const int totalNumbers = 50_000_000;
        const int fileCount = 8;
        const int numbersPerFile = totalNumbers / fileCount;

        const int minValue = -1_000_000;
        const int maxValue = 1_000_000;

        string directory = Directory.GetCurrentDirectory();

        string mainPath = Path.Combine(directory, "numbers.txt");

        Random random = new Random();

        using StreamWriter mainWriter = new StreamWriter(mainPath);

        for (int fileIndex = 1; fileIndex <= fileCount; fileIndex++)
        {
            string partPath = Path.Combine(directory, $"numbers{fileIndex}.txt");

            using StreamWriter partWriter = new StreamWriter(partPath);

            for (int i = 0; i < numbersPerFile; i++)
            {
                int number = random.Next(minValue, maxValue + 1);

                mainWriter.WriteLine(number);
                partWriter.WriteLine(number);
            }

            Console.WriteLine($"Generated numbers{fileIndex}.txt");
        }


        Console.WriteLine("Generation Finished..");
    }

    public void TaskOneAUseCase()
    {
        Console.WriteLine("Running Task 1A...");

        foreach (int threads in new[] { 1, 2, 4, 8, 16, 32, 64 })
        {
            string reportPath = Path.GetTempFileName();

            try
            {
                using var process = new Process();

                process.StartInfo.FileName = "/usr/bin/time";
                process.StartInfo.ArgumentList.Add("-l");
                process.StartInfo.ArgumentList.Add("-o");
                process.StartInfo.ArgumentList.Add(reportPath);

                process.StartInfo.ArgumentList.Add("dotnet");
                process.StartInfo.ArgumentList.Add(
                    Path.GetFullPath("./artifacts/TaskOneA/TaskOneA.dll")
                );
                process.StartInfo.ArgumentList.Add(threads.ToString());

                process.StartInfo.UseShellExecute = false;

                _metricCollectorTaskOneA.Collect(process, threads);

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception("TaskOneA failed.");

                string line = File.ReadLines(reportPath)
                    .First(l => l.Contains("maximum resident set size"));

                long peakMemoryBytes = long.Parse(
                    line.Split((char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries)[0]
                );

                _metricCollectorTaskOneA.StopCollect(threads, peakMemoryBytes);
            }
            finally
            {
                File.Delete(reportPath);
            }
        }

        Console.WriteLine("Task 1A Finished.");
    }

    public void TaskOneBUseCase()
    {
        Console.WriteLine("Running Task 1B...");

        Console.WriteLine("Task 1B Finished.");
    }

    public void TaskOneCUseCase()
    {
        Console.WriteLine("Running Task 1C...");

        foreach (int threads in new[] { 1, 2, 4, 8, 16, 32, 64 })
        {
            string reportPath = Path.GetTempFileName();

            try
            {
                using var process = new Process();

                process.StartInfo.FileName = "/usr/bin/time";
                process.StartInfo.ArgumentList.Add("-l");
                process.StartInfo.ArgumentList.Add("-o");
                process.StartInfo.ArgumentList.Add(reportPath);

                process.StartInfo.ArgumentList.Add("dotnet");
                process.StartInfo.ArgumentList.Add(
                    Path.GetFullPath("./artifacts/TaskOneC/TaskOneC.dll")
                );
                process.StartInfo.ArgumentList.Add(threads.ToString());

                process.StartInfo.UseShellExecute = false;

                _metricCollectorTaskOneC.Collect(process, threads);

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception("TaskOneC failed.");

                string line = File.ReadLines(reportPath)
                    .First(l => l.Contains("maximum resident set size"));

                long peakMemoryBytes = long.Parse(
                    line.Split((char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries)[0]
                );

                _metricCollectorTaskOneC.StopCollect(threads, peakMemoryBytes);
            }
            finally
            {
                File.Delete(reportPath);
            }
        }

        Console.WriteLine("Task 1C Finished.");
    }

    public void TaskTwoUseCase()
    {
        Console.WriteLine("Running Task 2...");

        Console.WriteLine("Task 2 Finished.");
    }

}

class Program
{
    static void Main(string[] args)
    {

        var matricCollectorTaskOneA = new MetriccolelctorTaskOneA();
        var matricCollectorTaskOneC = new MetriccolelctorTaskOneC();

        var app = new Application(matricCollectorTaskOneA, matricCollectorTaskOneC);

        while (true)
        {
            drawMenu();
            var option = chooseOption();
            Console.Clear();
            switch (option)
            {
                case 'x':
                    return;

                case 'g':
                    app.GenerateFileUseCase();
                    break;

                case '1':
                    app.TaskOneAUseCase();
                    break;

                case '2':
                    app.TaskOneBUseCase();
                    break;

                case '3':
                    app.TaskOneCUseCase();
                    break;

                case '4':
                    app.TaskTwoUseCase();
                    break;

                case 'd':
                    Console.WriteLine("Drawing results...");

                    Console.WriteLine("Results for TaskOneA");
                    foreach (var metric in matricCollectorTaskOneA.ExportMetrics())
                    {
                        Console.WriteLine(metric);
                    }

                    Console.WriteLine("Results for TaskOneC");
                    foreach (var metric in matricCollectorTaskOneC.ExportMetrics())
                    {
                        Console.WriteLine(metric);
                    }
                    break;

                case 'a':
                    Console.WriteLine("Running all tasks...");
                    break;
            }
        }
    }

    private static void drawMenu()
    {
        Console.WriteLine("-------MENU-------");
        Console.WriteLine("Option x: Close program.");
        Console.WriteLine("Option g: Generate a file with random numbers.");
        Console.WriteLine("Option 1: Run Task 1 A.");
        Console.WriteLine("Option 2: Run Task 1 B.");
        Console.WriteLine("Option 3: Run Task 1 C.");
        Console.WriteLine("Option 4: Run Task 2.");
        Console.WriteLine("Option d: Drow Results.");
        Console.WriteLine("Option a: Run all tasks, then drow results");
        Console.WriteLine("-------END MENU----");


    }

    private static char chooseOption()
    {
        while (true)
        {
            try
            {
                Console.WriteLine("------Choose option: -------");
                Console.Write("Option: ");

                string? input = Console.ReadLine()?.Trim().ToLowerInvariant();

                if (string.IsNullOrEmpty(input) || input.Length != 1)
                    throw new ArgumentException("Enter exactly one character.");

                char option = input[0];

                if (!"x1234dag".Contains(option))
                    throw new ArgumentException($"Unknown option: {option}");

                Console.WriteLine($"-----Option {option} choosed-----");
                return option;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}