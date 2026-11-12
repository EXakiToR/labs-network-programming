namespace CoffeeShop.Network.ServerStates;

public abstract class ServerState
{
    protected Server _server;
    protected CancellationTokenSource _cts;

    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public ServerState(Server server)
    {
        if (server is null)
            throw new ArgumentNullException();
        this._server = server;
        this._cts = new CancellationTokenSource(); // not accept, nned tp create new here AJKSNDFKJANFANS
    }

    // uses _cts
    public abstract Task Run();

    public void Cancel()
    {
        this._cts.Cancel();
    }
}
