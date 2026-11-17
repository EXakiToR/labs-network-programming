namespace CoffeeShop.Network.ServerStates;

public sealed class ServerDemandingState : ServerState
{
    public ServerDemandingState(Server server)
        : base(server) { }

    public override async Task Run()
    {
        try
        {
            await Task.Delay(Timeout.Infinite, _cts.Token);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested) { }
    }
}
