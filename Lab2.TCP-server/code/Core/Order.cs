using CoffeeShop.Network;

namespace CoffeeShop.Core;

public sealed class Order
{
    private readonly TaskCompletionSource _acknowledged = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenRegistration _disconnectRegistration;
    private int _resolved;

    public int Id { get; }
    public IReadOnlyDictionary<string, int> Items { get; }
    public ClientConnection Client { get; }
    public long PreparationSeconds { get; }

    internal Order(int id, Dictionary<string, int> items, ClientConnection client)
    {
        Id = id;
        Items = items;
        Client = client;
        PreparationSeconds = Items.Sum(item => (long)GetSeconds(item.Key) * item.Value);
        _disconnectRegistration = Client.Disconnected.Register(ReturnIfPending);
    }

    internal static int GetSeconds(string product) =>
        (int)Enum.Parse<PreparationTime>(product, ignoreCase: true);

    internal void Acknowledge() => _acknowledged.TrySetResult();

    internal void Cancel()
    {
        ReturnIfPending();
        _disconnectRegistration.Unregister();
    }

    private void ReturnIfPending()
    {
        if (Interlocked.Exchange(ref _resolved, 1) == 0)
        {
            ReturnStock();
            _acknowledged.TrySetCanceled();
        }
    }

    private void ReturnStock()
    {
        Stock.Return(Items);
        Client.Log($"Order #{Id} cancelled. Products returned to stock.");
    }

    internal async Task SendReadyAsync()
    {
        await _acknowledged.Task;

        // This worker owns the reservation through the send. A failed send returns it.
        if (Interlocked.Exchange(ref _resolved, 1) != 0)
            return;

        _disconnectRegistration.Unregister();
        try
        {
            await Client.SendMessageAsync($"Order #{Id} is ready");
        }
        catch
        {
            ReturnStock();
            throw;
        }

        try
        {
            Stock.Complete(Items);
        }
        finally
        {
            Client.Log($"Order #{Id} ready");
        }
    }
}
