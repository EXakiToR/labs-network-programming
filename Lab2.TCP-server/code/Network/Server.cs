using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using CoffeeShop.Core;
using CoffeeShop.Helpers;
using CoffeeShop.Network.ServerStates;

namespace CoffeeShop.Network;

public sealed class Server
{
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _alertLock = new(1, 1);
    private readonly ConcurrentDictionary<ClientConnection, byte> _clientConnections = new();
    private ServerState _state;

    public static Server Instance { get; } = new();

    internal Socket Listener { get; } = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    internal bool IsShuttingDown
    {
        get
        {
            lock (_stateLock)
                return _state is ServerShutDownState;
        }
    }

    private Server()
    {
        _state = new ServerAcceptingState(this);
    }

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

    public ICollection<ClientConnection> GetClientConnectionsSnapshot() => _clientConnections.Keys;

    private void GoToState(ServerState nextState)
    {
        lock (_stateLock)
        {
            if (_state is ServerShutDownState || _state.GetType() == nextState.GetType())
                return;

            ServerState previous = _state;
            _state = nextState;
            previous.Cancel();
        }
    }

    public async Task Run(int port = 5050)
    {
        using var listener = Listener;
        listener.Bind(new IPEndPoint(IPAddress.Any, port));
        listener.Listen();

        var workers = new Workers(Environment.ProcessorCount);
        Console.WriteLine($"Listening on port {port} with {Environment.ProcessorCount} workers");

        using var monitoring = new CancellationTokenSource();
        Task monitor = MemoryMonitoringTask(monitoring.Token);
        Console.CancelKeyPress += OnCancelKeyPress;
        try
        {
            while (true)
            {
                ServerState state;
                lock (_stateLock)
                    state = _state;

                await state.Run();
                if (state is ServerShutDownState)
                    break;
            }
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            monitoring.Cancel();
            await monitor;
            await StopClientConnectionsAsync();
            workers.Stop();
            await workers.WaitAsync();
        }
    }

    private async Task StopClientConnectionsAsync()
    {
        var connections = GetClientConnectionsSnapshot();
        foreach (var connection in connections)
            connection.Cancel();

        await Task.WhenAll(connections.Select(connection => connection.Completion));
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        GoToState(new ServerShutDownState(this));
    }

    internal async Task NotifyProvidersAsync()
    {
        await _alertLock.WaitAsync();
        try
        {
            var providers = GetClientConnectionsSnapshot()
                .Where(connection => connection.Role == ClientRole.DeliveryPerson)
                .ToArray();
            foreach (var alert in Stock.GetPendingAlerts())
            {
                bool delivered = false;
                foreach (var provider in providers)
                {
                    try
                    {
                        await provider.SendMessageAsync($"Low stock: {alert.Product}, {alert.Available} left");
                        provider.Log($"Low stock alert sent for {alert.Product}");
                        delivered = true;
                    }
                    catch (Exception ex)
                    {
                        provider.Log($"Could not send low stock alert: {ex.Message}");
                    }
                }

                if (delivered)
                    Stock.AcknowledgeAlert(alert);
            }
        }
        finally
        {
            _alertLock.Release();
        }
    }

    public async Task MemoryMonitoringTask(CancellationToken cancellationToken = default)
    {
        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                double heapUsagePercentage = MemoryManager.GetHeapUsagePercentage();
                if (heapUsagePercentage > 90.0)
                    GoToState(new ServerShutDownState(this));
                else if (heapUsagePercentage > 80.0)
                    GoToState(new ServerDemandingState(this));
                else if (heapUsagePercentage < 70.0)
                    GoToState(new ServerAcceptingState(this));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Console.WriteLine($"Memory monitoring error: {ex.Message}");
        }
    }
}
