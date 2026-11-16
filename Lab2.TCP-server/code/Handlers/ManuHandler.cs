namespace CoffeeShop.Handlers;

using CoffeeShop.Core;
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
            if (client.Role != ClientRole.RegisteredUser)
            {
                await client.SendMessageAsync("You are not registered as a user");
                return;
            }

            MenuRequest? menuRequest = payload.ToObject<MenuRequest>();

            Dictionary<string, int> stockSnapshot = Stock.GetSnapshot();

            foreach (var item in stockSnapshot)
            {
                int time;
                if (item.Key.Equals("coffee", StringComparison.OrdinalIgnoreCase))
                {
                    time = 5;
                }
                else if (item.Key.Equals("tea", StringComparison.OrdinalIgnoreCase))
                {
                    time = 3;
                }
                else if (item.Key.Equals("croissant", StringComparison.OrdinalIgnoreCase))
                {
                    time = 7;
                }
                else
                {
                    time = 0; // Default preparation time for unknown items
                }
                await client.SendMessageAsync(
                    $"{item.Key}: {item.Value} in stock, {time} secs to prepare each."
                );
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing order: {ex.Message}");
            await client.SendMessageAsync("Invalid menuRequest JSON");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling menu request: {ex.Message}");
            await client.SendMessageAsync("Error processing menu request");
        }
    }
}
