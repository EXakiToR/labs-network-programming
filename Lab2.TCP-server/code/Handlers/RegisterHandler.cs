namespace CoffeeShop.Handlers;

using CoffeeShop.Network;
using CoffeeShop.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class RegisterHandler
{
    public static async Task HandleAsync(JToken payload, ClientConnection client)
    {
        try
        {
            RegisterRequest? registerRequest = payload.ToObject<RegisterRequest>();

            if (client.Role != ClientRole.Undefined)
            {
                await client.SendMessageAsync("You are already registered");
                return;
            }

            string role = registerRequest?.Role.ToLower() ?? string.Empty;
            if (role == "client")
            {
                client.Role = ClientRole.RegisteredUser;
            }
            else if (role == "provider")
            {
                client.Role = ClientRole.DeliveryPerson;
            }
            else
            {
                await client.SendMessageAsync("Invalid role specified");
                return;
            }
            await client.SendMessageAsync("Register request received");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing order: {ex.Message}");
            await client.SendMessageAsync("Invalid menuRequest JSON");
        }
    }
}
