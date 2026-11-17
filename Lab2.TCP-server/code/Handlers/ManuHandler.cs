using CoffeeShop.Core;
using CoffeeShop.Network;
using Newtonsoft.Json.Linq;

namespace CoffeeShop.Handlers;

public static class MenuHandler
{
    public static async Task HandleAsync(JToken payload, ClientConnection client)
    {
        if (client.Role != ClientRole.RegisteredUser)
        {
            client.Log("Menu request refused. Connection is not registered as a client");
            await client.SendMessageAsync("You are not registered as a user");
            return;
        }

        foreach (var (product, quantity) in Stock.GetSnapshot())
        {
            await client.SendMessageAsync(
                $"{product}: {quantity} in stock, {Order.GetSeconds(product)} secs to prepare each."
            );
        }
    }
}
