namespace CoffeeShop.Network.ServerStates;

public sealed class ServerShutDownState : ServerState
{
    public ServerShutDownState(Server server)
        : base(server) { }

    public override Task Run()
    {
        _server.Listener.Dispose();
        return Task.CompletedTask;
    }
}
