namespace CoffeeShop.Network.ServerStates;

public sealed class ServerShutDownState : ServerState
{
    public ServerShutDownState(Server server)
        : base(server) { }

    public override async Task Run()
    {
        _server.Listener.Dispose();
        Console.WriteLine("Server is shutting down. Waiting 10 seconds for accepted work to finish.");
        await Task.Delay(TimeSpan.FromSeconds(10));
    }
}
