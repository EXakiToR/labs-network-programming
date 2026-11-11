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
            try
            {
                connection = await this._server._listener.AcceptAsync();
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Listener socket has been disposed. Exiting accepting loop.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting connection: {ex.Message}");
                continue;
            }

            Console.WriteLine("Connection accepted!");
            var conn = new ClientConnection(connection);
            this._server.AddClientConnection(conn);
            _ = conn.HandleAsync();
        }
    }
}
