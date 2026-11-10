namespace CoffeeShop.Network;

using CoffeeShop.Helpers;

public static class ServerState
{
    private static int _status = (int)ServerStatus.Accepting;

    public static ServerStatus Status => (ServerStatus)Volatile.Read(ref _status);

    private static void setStatus(ServerStatus status)
    {
        Volatile.Write(ref _status, (int)status);
    }

    public static async void MonitorMemoryAsync(CancellationTokenSource cts)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(cts.Token))
        {
            double heapUsage = MemoryManager.GetHeapUsagePercentage();
            // every 2 seconds, check memory usage and update server status accordingly

            if (heapUsage > 90)
            {
                Console.WriteLine("Memory usage is above 90%, shutting down server.");
                cts.Cancel();
                break;
            }
            else if (heapUsage > 80)
            {
                Console.WriteLine(
                    "too much memory usage, stop connecting new clients. continue serving existing clients."
                );
                ServerState.setStatus(ServerStatus.Draining);
            }
            else if (heapUsage > 75)
            {
                Console.WriteLine("Warning: Memory usage is above 70%.");
            }
            else if (heapUsage < 70)
            {
                ServerState.setStatus(ServerStatus.Accepting);
            }
        }
    }
}

public enum ServerStatus
{
    Accepting, // Normal operation, accepting new connections
    Draining, // Stop accepting, finish existing connections
}
