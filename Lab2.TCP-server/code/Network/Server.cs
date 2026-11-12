using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using CoffeeShop.Helpers;
using CoffeeShop.Network.ServerStates;

namespace CoffeeShop.Network;

// Singletone class
public sealed class Server
{
    private ServerState _state;

    // Acceoting - get new connections.
    // Demanding - stop accepting new connections.
    // Shutting Down - give 5 sec and shutdown server.

    public static Server Instance { get; } = new Server();

    private readonly ConcurrentDictionary<ClientConnection, byte> _clientConnections = new();
    public Socket _listener;

    public void AddClientConnection(ClientConnection conn)
    {
        if (!_clientConnections.TryAdd(conn, 0))
            throw new InvalidOperationException("Connection already registered.");
    }

    public void RemoveClientConnection(ClientConnection conn)
    {
        if (!_clientConnections.TryRemove(conn, out _))
            throw new InvalidOperationException("Connection not found.");
    }

    public ICollection<ClientConnection> GetClientConnectionsSnapshot()
    {
        return _clientConnections.Keys;
    }

    private Server()
    {
        this._state = new ServerAcceptingState(this);
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true; // Prevent the process from terminating.
            await this._state.GoToState(new ServerShutDownState(this));
        };

        _ = MemoryMonitoringTask();
        this._listener = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        this._listener.Bind(new IPEndPoint(IPAddress.Any, 5050));
        this._listener.Listen();
    }

    public void SetState(ServerState state)
    {
        this._state = state;
    }

    private async Task GoToState(ServerState state)
    {
        await this._state.GoToState(state);
    }

    public async Task Run()
    {
        await this._state.Run();
    }

    public async Task MemoryMonitoringTask(CancellationToken cancellationToken = default)
    {
        try
        {
            while (true)
            {
                await Task.Delay(10000, cancellationToken); // every 10 seconds check memory usage.

                double heapUsagePercentage = MemoryManager.GetHeapUsagePercentage();

                // Evaluate memory thresholds.
                if (heapUsagePercentage > 90.0)
                {
                    await this._state.GoToState(new ServerShutDownState(this));
                }
                else if (heapUsagePercentage > 80.0)
                {
                    await this._state.GoToState(new ServerDemandingState(this));
                }
                else if (heapUsagePercentage < 70.0)
                {
                    await this._state.GoToState(new ServerAcceptingState(this));
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal cancellation.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Memory monitoring task encountered an error: {ex.Message}");
        }
    }
}
