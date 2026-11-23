using System.Net;

namespace LanChat.Network.Peers;

public sealed record Peer(
    Guid Id, string Name, IPEndPoint EndPoint,
    DateTimeOffset RegisteredAt, DateTimeOffset LastSeen);
