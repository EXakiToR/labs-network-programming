namespace CoffeeShop.Core;

public sealed class Workers
{
    private int _count = 0;

    Thread[] workers;

    Workers(int count)
    {
        _count = count;
        workers = new Thread[count];
    }

    public void AddWorker()
    {
        Interlocked.Increment(ref _count);
    }

    public void RemoveWorker()
    {
        Interlocked.Decrement(ref _count);
    }

    private void WorkerLoop()
    {
        while (true)
        {
            // get order from queue
            // process order
            // update stock
            // send response to client
        }
    }
}
