namespace CoffeeShop.Core;

using System.Threading;
using CoffeeShop.Core;
using CoffeeShop.Network;

public sealed class Workers
{
    private readonly Task[] workers;

    Workers(int count)
    {
    }

    public void Wait()
    {
    }

    public void Stop()
    {
    }

    private void WorkerLoop()
    {
        while (true)
        {
            // get order from queue
            Order order = OrderQueue.Dequeue();
            ClientConnection client = order.Client;

            // check if can process order (check stock)
            Dictionary<string, int> orderItems = Stock.GetSnapshot();
            foreach (var (product, quantity) in order.Items)
            {
                if (!orderItems.TryGetValue(product, out int available) || available < quantity)
                {
                    // handle insufficient stock
                    Console.WriteLine($"Insufficient stock for product: {product}");
                    // send response to client about insufficient stock
                    continue;
                }
            }

            try
            {
                foreach (var (product, quantity) in order.Items)
                {
                    if (!Stock.TryTake(new Dictionary<string, int> { { product, quantity } }))
                    {
                        // handle insufficient stock
                        Console.WriteLine($"Insufficient stock for product: {product}");
                        // send response to client about insufficient stock

                        // actual preparation with waiting (Thread,Sleep simulates preparing)
                        for (int i = 0; i < quantity; i++)
                        {
                            Console.WriteLine($"Preparing {product} for client");
                            Thread.Sleep(...); // Simulate time taken to prepare each item
                        }
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing order: {ex.Message}");
                // send response to client about error
            }
            // process order
            // update stock
            // send response to client
        }
    }
}
