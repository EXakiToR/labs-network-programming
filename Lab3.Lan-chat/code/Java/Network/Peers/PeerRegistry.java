package LanChat.Network.Peers;
import java.time.*;
import java.util.*;

public final class PeerRegistry {
    private final Map<UUID, Peer> peers = new HashMap<>();
    public synchronized List<Peer> addOrUpdate(Peer peer) {
        Peer existing = peers.get(peer.id());
        if (existing != null) peer = new Peer(peer.id(), peer.name(), peer.endPoint(),
                existing.registeredAt(), peer.lastSeen().isAfter(existing.lastSeen()) ? peer.lastSeen() : existing.lastSeen());
        peers.put(peer.id(), peer);
        return resolveConflicts();
    }
    private List<Peer> resolveConflicts() {
        Map<String, Peer> winners = new HashMap<>();
        List<Peer> losers = new ArrayList<>();
        // UUID text order matches Guid.CompareTo's unsigned component ordering.
        List<Peer> ordered = peers.values().stream().sorted(Comparator.comparing(Peer::registeredAt)
                .thenComparing(peer -> peer.id().toString())).toList();
        for (Peer peer : ordered) {
            if (winners.putIfAbsent(peer.name(), peer) != null) losers.add(peer);
        }
        losers.forEach(peer -> peers.remove(peer.id()));
        return losers;
    }
    public synchronized Peer find(String name) {
        return peers.values().stream().filter(peer -> peer.name().equals(name)).findFirst().orElse(null);
    }
    public synchronized Peer find(UUID id) { return peers.get(id); }
    public synchronized List<Peer> getAll() { return new ArrayList<>(peers.values()); }
    public synchronized Peer remove(UUID id) { return peers.remove(id); }
    public synchronized void updateLastSeen(UUID id, Instant now) {
        Peer peer = peers.get(id);
        if (peer != null) peers.put(id, new Peer(id, peer.name(), peer.endPoint(), peer.registeredAt(), now));
    }
    public synchronized List<Peer> removeExpired(Instant now, Duration timeout) {
        List<Peer> expired = peers.values().stream()
                .filter(peer -> !peer.lastSeen().plus(timeout).isAfter(now)).toList();
        expired.forEach(peer -> peers.remove(peer.id()));
        return expired;
    }
}
