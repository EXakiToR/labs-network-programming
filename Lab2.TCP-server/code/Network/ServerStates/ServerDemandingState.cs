namespace CoffeeShop.Network.ServerStates;

public sealed class ServerDemandingState : ServerState
{
    public ServerDemandingState(Server server)
        : base(server) { }

    public override async Task Run()
    {
        // don't accept new connections, so, basicly, do notjng
        await Task.Delay(Timeout.Infinite, _cts.Token);
    }
}
