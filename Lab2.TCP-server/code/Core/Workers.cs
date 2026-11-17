namespace CoffeeShop.Core;

public sealed class Workers
{
    private readonly CancellationTokenSource _stop = new();
    private readonly Task[] _workers;

    public Workers(int count)
    {
        _workers = new Task[count];
        for (int i = 0; i < count; i++)
            _workers[i] = WorkerLoopAsync(_stop.Token);
    }

    public Task WaitAsync() => Task.WhenAll(_workers);

    public void Stop() => _stop.Cancel();

    private static async Task WorkerLoopAsync(CancellationToken stopping)
    {
        try
        {
            await foreach (var order in OrderQueue.ReadAllAsync(stopping))
            {
                if (stopping.IsCancellationRequested)
                    break;

                var disconnected = order.Client.Disconnected;
                if (disconnected.IsCancellationRequested)
                    continue;

                try
                {
                    foreach (var (product, quantity) in order.Items)
                    {
                        var delay = TimeSpan.FromSeconds(Order.GetSeconds(product));
                        for (int i = 0; i < quantity; i++)
                            await Task.Delay(delay, disconnected);
                    }

                    await order.SendReadyAsync();
                }
                catch (OperationCanceledException) when (disconnected.IsCancellationRequested)
                {
                    order.Cancel();
                }
                catch (Exception ex)
                {
                    order.Cancel();
                    order.Client.Log($"Error processing order #{order.Id}: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }
}
