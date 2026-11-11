namespace CoffeeShop.Network.ServerStates;

public sealed class ServerShutDownState : ServerState
{
    public ServerShutDownState(Server server)
        : base(server) { }

    public override async Task Run()
    {
        // exit point of the project
        // TODO: notify all clientConnections, give 10 seconds delay, then kill them, and terminate the process
        this._server._listener.Dispose();
    }
}
