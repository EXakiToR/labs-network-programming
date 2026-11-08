using System.Threading.Channels;

namespace CoffeeShop.Core;

public static class OrderQueue
{
    private static readonly Channel<Order> _queue = Channel.CreateUnbounded<Order>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false }
    );

    public static ValueTask EnqueueAsync(Order order)
    {
        return _queue.Writer.WriteAsync(order);
    }

    public static IAsyncEnumerable<Order> ReadAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }
}
