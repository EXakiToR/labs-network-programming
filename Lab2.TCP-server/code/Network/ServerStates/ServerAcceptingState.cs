namespace CoffeeShop.Network.ServerStates;

using System.Net.Sockets;

public sealed class ServerAcceptingState : ServerState
{
    public ServerAcceptingState(Server server)
        : base(server) { }

    public override async Task Run()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            Socket connection;
            connection = await this._server._listener.AcceptAsync();
            var conn = new ClientConnection(connection);
            this._server.clientConnections.Add(conn);
        }
    }
}
