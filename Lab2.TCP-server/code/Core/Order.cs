namespace CoffeeShop.Core;

public sealed class Order
{
    public int Id { get; init; }

    public required Dictionary<string, int> Items { get; init; }

    // public required ClientConnection Client { get; init; }

    public CancellationTokenSource Cancellation { get; } = new();
}
