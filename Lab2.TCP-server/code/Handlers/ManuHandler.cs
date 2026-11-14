namespace CoffeeShop.Handlers;

using CoffeeShop.Network;
using CoffeeShop.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class MenuHandler
{
    public static async Task HandleAsync(JToken payload, ClientConnection client)
    {
        try
        {
            MenuRequest? menuRequest = payload.ToObject<MenuRequest>();

            await client.SendMessageAsync("Menu request received");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing order: {ex.Message}");
            await client.SendMessageAsync("Invalid menuRequest JSON");
        }
    }
}
