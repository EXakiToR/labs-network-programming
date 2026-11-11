namespace CoffeeShop.Network.ServerStates;

public sealed class ServerShutDownState : ServerState
{
    public ServerShutDownState(Server server)
        : base(server) { }

    public override async Task Run()
    {
        // exit point of the project
        this._server.clientConnections.ToList().ForEach(conn => conn.Cancel());
        await Task.Delay(10000); // wait 10 seconds before shutting down the server

        this._server._listener.Dispose();
        //after exiting from here, we will be in the Program.cs, and the process will terminate, because we are in the main thread.
    }
}
