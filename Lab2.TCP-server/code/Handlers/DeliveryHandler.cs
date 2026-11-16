namespace CoffeeShop.Handlers;

using CoffeeShop.Core;
using CoffeeShop.Network;
using CoffeeShop.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class DeliveryHandler
{
    public static async Task HandleAsync(JToken payload, ClientConnection client)
    {
        try
        {
            if (client.Role != ClientRole.DeliveryPerson)
            {
                await client.SendMessageAsync("You are not registered as a delivery person");
                return;
            }
            DeliveryRequest? deliveryRequest = payload.ToObject<DeliveryRequest>();
            foreach (OrderItem item in deliveryRequest!.Items)
            {
                Stock.Add(new Dictionary<string, int> { { item.Product, item.Quantity } });
            }
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

    private static bool IsValidDelivery(OrderRequest? order)
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
