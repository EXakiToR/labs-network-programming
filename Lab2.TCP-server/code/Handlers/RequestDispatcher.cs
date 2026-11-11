namespace CoffeeShop.Handlers;

using CoffeeShop.Network;
using CoffeeShop.Requests;

public static class RequestDispatcher
{
    //private readonly OrderHandler _orderHandler;
    //private readonly MenuHandler _menuHandler;

    // public RequestDispatcher(OrderHandler orderHandler, MenuHandler menuHandler)
    // {
    //     _orderHandler = orderHandler;
    //     _menuHandler = menuHandler;
    // }

    public static Task DispatchAsync(RequestEnvelope request, ClientConnection client)
    {
        // switch (request.Type.ToLower())
        // {
        //     case "order":
        //         await OrderHandler.HandleOrderAsync(request.Payload, client);
        //         break;
        //     //classe "menu":
        //     //await _menuHandler.HandleMenuRequestAsync(client);
        //     //     break;
        //     default:
        //         Console.WriteLine($"Unknown request type: {request.Type}");
        //         break;
        // }
        Console.WriteLine($"Received request of type: {request.Type}");
        return Task.CompletedTask;
    }
}
