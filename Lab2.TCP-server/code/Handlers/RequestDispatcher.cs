using CoffeeShop.Network;
using CoffeeShop.Requests;

namespace CoffeeShop.Handlers;

public static class RequestDispatcher
{
    public static Task DispatchAsync(RequestEnvelope request, ClientConnection client) =>
        request.Type switch
        {
            "order" => OrderHandler.HandleAsync(request.Payload, client),
            "menu" => MenuHandler.HandleAsync(request.Payload, client),
            "register" => RegisterHandler.HandleAsync(request.Payload, client),
            "delivery" => DeliveryHandler.HandleAsync(request.Payload, client),
            _ => client.SendMessageAsync("Unknown request type"),
        };
}
