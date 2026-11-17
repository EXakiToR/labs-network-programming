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
                connection = await _server.Listener.AcceptAsync(_cts.Token);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Listener socket has been disposed. Exiting accepting loop.");
                break;
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                Console.WriteLine("Accepting new connections has been canceled.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting connection: {ex.Message}");
                continue;
            }

            try
            {
                var conn = new ClientConnection(connection);
                _server.AddClientConnection(conn);
                conn.Start();
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Error opening accepted connection: {ex.Message}");
                connection.Dispose();
            }
        }
    }
}
