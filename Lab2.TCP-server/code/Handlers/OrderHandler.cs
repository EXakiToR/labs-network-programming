namespace CoffeeShop.Handlers;

using CoffeeShop.Core;
using CoffeeShop.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class OrderHandler
{
    public static async Task HandleOrderAsync(JToken payload, ClientConnection client)
    {
        try
        {
            OrderRequest? orderRequest = payload.ToObject<OrderRequest>();

            if (!IsValidOrder(orderRequest))
            {
                await client.SendMessageAsync("Invalid order");
                return;
            }

            foreach (OrderItem item in orderRequest!.Items)
            {
                Console.WriteLine($"{item.Product}: {item.Quantity}");
            }

            // At this point, we collected the data of items. But,
            // stock can not have such, need to check the stock
            var stockSnapshot = Stock.GetSnapshot();
            foreach (OrderItem item in orderRequest.Items)
            {
                if (!stockSnapshot.TryGetValue(item.Product, out int available) || available < item.Quantity)
                {
                    await client.SendMessageAsync($"Not enough stock for {item.Product}");
                    return;
                }
            }
            // at this point, we know that stock has enough for all items in the order.
            // now, put in the Queue.
            

            // then, need to answer to client about that is it ok or not.
            // and, if all ok, only then put into a queue the order.

            await client.SendMessageAsync("Order received");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing order: {ex.Message}");
            await client.SendMessageAsync("Invalid order JSON");
        }
    }

    private static readonly HashSet<string> AllowedProducts = new(StringComparer.OrdinalIgnoreCase)
    {
        "coffee",
        "tea",
        "croissant",
    };

    private static bool IsValidOrder(OrderRequest? order)
    {
        if (order?.Items is null || order.Items.Count == 0)
            return false;

        foreach (OrderItem? item in order.Items)
        {
            if (item is null)
                return false;

            if (string.IsNullOrWhiteSpace(item.Product))
                return false;

            if (!AllowedProducts.Contains(item.Product))
                return false;

            if (item.Quantity <= 0)
                return false;
        }

        return true;
    }
}
