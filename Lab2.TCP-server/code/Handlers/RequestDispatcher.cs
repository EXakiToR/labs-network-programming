namespace CoffeeShop.Handlers;

using System.Net.Sockets;
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

    public static async Task DispatchAsync(RequestEnvelope request, Socket connection)
    {
        switch (request.Type.ToLower())
        {
            case "order":
                await OrderHandler.HandleOrderAsync(request.Payload, connection);
                break;
            //classe "menu":
            //await _menuHandler.HandleMenuRequestAsync(connection);
            //     break;
            default:
                Console.WriteLine($"Unknown request type: {request.Type}");
                break;
        }
    }
}
