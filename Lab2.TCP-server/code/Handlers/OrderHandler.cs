using CoffeeShop.Core;
using CoffeeShop.Network;
using CoffeeShop.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoffeeShop.Handlers;

public static class OrderHandler
{
    private static readonly object _acceptanceLock = new();
    private static int _lastOrderId;

    public static async Task HandleAsync(JToken payload, ClientConnection client)
    {
        if (client.Role != ClientRole.RegisteredUser)
        {
            client.Log("Order refused. Connection is not registered as a client");
            await client.SendMessageAsync("You are not registered as a user");
            return;
        }

        Dictionary<string, int> items;
        try
        {
            items = OrderItem.GetQuantities(payload.ToObject<OrderRequest>()!.Items);
        }
        catch (Exception ex) when (ex is JsonException or OverflowException)
        {
            client.Log($"Order refused. Invalid quantity or payload: {ex.Message}");
            await client.SendMessageAsync("Invalid order");
            return;
        }

        Order? order = null;
        (string Product, int Available)? shortage;
        bool hasNewAlert;
        lock (_acceptanceLock)
        {
            shortage = Stock.Reserve(items, out hasNewAlert);
            if (shortage is null)
            {
                order = new Order(++_lastOrderId, items, client);
                OrderQueue.Enqueue(order);
            }
        }

        if (order is null)
        {
            var missing = shortage!.Value;
            string message = $"Not enough stock for {missing.Product}. {missing.Available} left";
            client.Log($"Order refused. {message}");
            await client.SendMessageAsync(message);
            return;
        }

        if (hasNewAlert)
            _ = Server.Instance.NotifyProvidersAsync();

        try
        {
            await client.SendMessageAsync($"Order #{order.Id} accepted. Preparation time: {order.PreparationSeconds} seconds");
            client.Log($"Order #{order.Id} accepted: {string.Join(", ", items.Select(item => $"{item.Value} {item.Key}"))}");
            order.Acknowledge();
        }
        catch
        {
            order.Cancel();
            throw;
        }
    }
}
