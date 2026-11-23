package LanChat.Network.Peers;
import java.net.InetSocketAddress;
import java.time.Instant;
import java.util.UUID;
public record Peer(UUID id, String name, InetSocketAddress endPoint,
                   Instant registeredAt, Instant lastSeen) { }
