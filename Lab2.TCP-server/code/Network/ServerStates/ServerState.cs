namespace CoffeeShop.Network.ServerStates;

public abstract class ServerState
{
    protected readonly Server _server;
    protected readonly CancellationTokenSource _cts = new();

    protected ServerState(Server server)
    {
        _server = server;
    }

    public abstract Task Run();

    public void Cancel() => _cts.Cancel();
}
