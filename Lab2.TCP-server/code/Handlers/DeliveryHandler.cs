using CoffeeShop.Core;
using CoffeeShop.Network;
using CoffeeShop.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoffeeShop.Handlers;

public static class DeliveryHandler
{
    public static async Task HandleAsync(JToken payload, ClientConnection client)
    {
        if (client.Role != ClientRole.DeliveryPerson)
        {
            client.Log("Delivery refused. Connection is not registered as a provider");
            await client.SendMessageAsync("You are not registered as a delivery person");
            return;
        }

        try
        {
            var items = OrderItem.GetQuantities(payload.ToObject<DeliveryRequest>()!.Items);
            Stock.Add(items);
            client.Log($"Delivery received: {string.Join(", ", items.Select(item => $"{item.Value} {item.Key}"))}");
        }
        catch (Exception ex) when (ex is JsonException or OverflowException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            client.Log($"Delivery error: {ex.Message}");
            await client.SendMessageAsync("Could not add delivery to stock");
            return;
        }

        await client.SendMessageAsync("Delivery added to stock");
    }
}
