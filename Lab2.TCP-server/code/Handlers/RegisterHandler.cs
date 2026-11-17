using CoffeeShop.Network;
using CoffeeShop.Requests;
using Newtonsoft.Json.Linq;

namespace CoffeeShop.Handlers;

public static class RegisterHandler
{
    public static async Task HandleAsync(JToken payload, ClientConnection client)
    {
        if (client.Role != ClientRole.Undefined)
        {
            client.Log("Registration refused. Connection is already registered");
            await client.SendMessageAsync("You are already registered");
            return;
        }

        string role = payload.ToObject<RegisterRequest>()!.Role;
        await client.SendMessageAsync($"Registered as {role}");
        client.Role = role == "provider" ? ClientRole.DeliveryPerson : ClientRole.RegisteredUser;
        client.Log($"Registered as {role}");
        if (client.Role == ClientRole.DeliveryPerson)
            _ = Server.Instance.NotifyProvidersAsync();
    }
}
