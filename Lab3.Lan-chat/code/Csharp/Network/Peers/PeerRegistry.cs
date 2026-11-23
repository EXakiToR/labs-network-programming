namespace LanChat.Network.Peers;

public sealed class PeerRegistry
{
    private readonly Dictionary<Guid, Peer> _peers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // The returned peers lost a name conflict. Main handles local shutdown.
    public List<Peer> AddOrUpdate(Peer peer)
    {
        _semaphore.Wait();
        try
        {
            if (_peers.TryGetValue(peer.Id, out Peer? existing))
                peer = peer with
                {
                    RegisteredAt = existing.RegisteredAt,
                    LastSeen = peer.LastSeen > existing.LastSeen ? peer.LastSeen : existing.LastSeen
                };
            _peers[peer.Id] = Copy(peer);
            return ResolveConflicts();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // Called while holding the semaphore, including on every insertion.
    private List<Peer> ResolveConflicts()
    {
        List<Peer> losers = _peers.Values
            .GroupBy(peer => peer.Name, StringComparer.Ordinal)
            .SelectMany(group => group.OrderBy(peer => peer.RegisteredAt).ThenBy(peer => peer.Id).Skip(1))
            .Select(Copy)
            .ToList();
        foreach (Peer peer in losers)
            _peers.Remove(peer.Id);
        return losers;
    }

    public Peer? Find(string name)
    {
        _semaphore.Wait();
        try
        {
            Peer? peer = _peers.Values.FirstOrDefault(peer => peer.Name == name);
            return peer is null ? null : Copy(peer);
        }
        finally { _semaphore.Release(); }
    }

    public Peer? Find(Guid id)
    {
        _semaphore.Wait();
        try { return _peers.TryGetValue(id, out Peer? peer) ? Copy(peer) : null; }
        finally { _semaphore.Release(); }
    }

    public List<Peer> GetAll()
    {
        _semaphore.Wait();
        try { return _peers.Values.Select(Copy).ToList(); }
        finally { _semaphore.Release(); }
    }

    public Peer? Remove(Guid id)
    {
        _semaphore.Wait();
        try { return _peers.Remove(id, out Peer? peer) ? Copy(peer) : null; }
        finally { _semaphore.Release(); }
    }

    public void UpdateLastSeen(Guid id, DateTimeOffset now)
    {
        _semaphore.Wait();
        try
        {
            if (_peers.TryGetValue(id, out Peer? peer))
                _peers[id] = peer with { LastSeen = now };
        }
        finally { _semaphore.Release(); }
    }

    public List<Peer> RemoveExpired(DateTimeOffset now, TimeSpan timeout)
    {
        _semaphore.Wait();
        try
        {
            List<Peer> expired = _peers.Values.Where(peer => now - peer.LastSeen >= timeout)
                .Select(Copy).ToList();
            foreach (Peer peer in expired)
                _peers.Remove(peer.Id);
            return expired;
        }
        finally { _semaphore.Release(); }
    }

    private static Peer Copy(Peer peer) => peer with
    {
        EndPoint = new(peer.EndPoint.Address, peer.EndPoint.Port)
    };
}
