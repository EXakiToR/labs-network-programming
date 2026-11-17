using System.Threading.Channels;

namespace CoffeeShop.Core;

public static class OrderQueue
{
    private static readonly Channel<Order> _queue = Channel.CreateUnbounded<Order>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false }
    );

    internal static void Enqueue(Order order)
    {
        if (!_queue.Writer.TryWrite(order))
            throw new InvalidOperationException("The order queue is closed.");
    }

    public static IAsyncEnumerable<Order> ReadAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }
}
