package LanChat;

import java.io.*;
import java.net.*;
import java.nio.file.Path;
import java.security.GeneralSecurityException;
import java.time.*;
import java.time.temporal.ChronoUnit;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.function.Supplier;
import LanChat.Network.*;
import LanChat.Network.Datagrams.*;
import LanChat.Network.Peers.*;

public final class Main {
    private final String name;
    private final UUID senderId = UUID.randomUUID();
    private String registeredAt = "0001-01-01T00:00:00+00:00";
    private final byte[] key;
    private final PeerRegistry peers = new PeerRegistry();
    private final CountDownLatch shutdown = new CountDownLatch(1);
    private final AtomicBoolean stopping = new AtomicBoolean();
    private final AtomicBoolean finished = new AtomicBoolean();
    private final DatagramCommunicator communicator;
    private final DatagramLogger logger;
    private volatile int exitCode;

    private Main(String name, int port, String passphrase, DatagramLogger logger)
            throws IOException, GeneralSecurityException {
        this.name = name;
        this.logger = logger;
        key = PrivateMessageDatagram.deriveKey(passphrase);
        communicator = new DatagramCommunicator(name, port, logger);
    }

    public static void main(String[] args) {
        int status = 0;
        try {
            if (args.length != 3 || args[0].isBlank()) throw new IllegalArgumentException();
            int port = Integer.parseInt(args[1]);
            if (port < 1024 || port > 65535) throw new IllegalArgumentException();
            Path log = Path.of("logs", "chat-" + port + "-" + UUID.randomUUID().toString().replace("-", "") + ".log");
            try (DatagramLogger logger = new DatagramLogger(log)) {
                System.out.println("Log: " + logger.filePath);
                Main program = new Main(args[0], port, args[2], logger);
                status = program.run();
            }
        } catch (IllegalArgumentException ex) {
            System.out.println("Usage: java -cp out LanChat.Main <name> <port:1024-65535> \"<passphrase>\"");
            status = 1;
        } catch (IOException | GeneralSecurityException ex) {
            System.err.println("Chat error: " + ex.getMessage());
            status = 1;
        } catch (InterruptedException ex) {
            Thread.currentThread().interrupt();
            status = 1;
        }
        System.exit(status);
    }

    private int run() throws IOException, InterruptedException {
        boolean joined = false;
        Thread hook = new Thread(this::finish, "shutdown");
        try {
            communicator.broadcast(create(NameRequestDatagram::new));
            List<Datagram> discovered = communicator.receiveFor(Duration.ofSeconds(1));
            for (Datagram datagram : discovered) {
                if (datagram.senderId.equals(senderId)) { logDrop(datagram, "Own datagram."); continue; }
                if (!(datagram instanceof NameResponseDatagram || datagram instanceof RegistrationRequestDatagram)) {
                    if (!(datagram instanceof NameRequestDatagram)) logDrop(datagram, "Ignored during discovery.");
                    continue;
                }
                if (datagram.name.equals(name)) {
                    System.out.println("Name '" + name + "' is occupied.");
                    return 1;
                }
                addPeer(datagram);
            }
            registeredAt = Instant.now().truncatedTo(ChronoUnit.MICROS).toString().replace("Z", "+00:00");
            Instant now = Instant.now();
            peers.addOrUpdate(new Peer(senderId, name, new InetSocketAddress("127.0.0.1",
                    communicator.getPrivatePort()), OffsetDateTime.parse(registeredAt).toInstant(), now));
            communicator.broadcast(create(RegistrationRequestDatagram::new));
            joined = true;
            for (Datagram request : discovered) {
                if (request instanceof NameRequestDatagram && !request.senderId.equals(senderId))
                    communicator.sendDatagram(create(NameResponseDatagram::new), request.remoteEndPoint);
            }
            System.out.println("Joined as " + name + ". Commands: /msg name text, /users, /quit.");
            Runtime.getRuntime().addShutdownHook(hook);
            start("keyboard", this::keyboardLoop);
            start("receiver", this::receiveLoop);
            start("heartbeat", this::heartbeatLoop);
            shutdown.await();
            return exitCode;
        } finally {
            if (joined) finish(); else communicator.close();
            try { Runtime.getRuntime().removeShutdownHook(hook); }
            catch (IllegalStateException ignored) { }
        }
    }
    private static void start(String name, Runnable action) {
        Thread thread = new Thread(action, name);
        thread.setDaemon(true);
        thread.start();
    }
    private <T extends Datagram> T create(Supplier<T> factory) {
        T datagram = factory.get();
        datagram.name = name;
        datagram.senderId = senderId;
        datagram.registeredAt = registeredAt;
        return datagram;
    }
    private void logDrop(Datagram datagram, String reason) {
        logger.write("DROP", datagram.remoteEndPoint, datagram.getType(), datagram.receivedSize, reason);
    }
    private void shutdownRequest(String reason) {
        if (!stopping.compareAndSet(false, true)) return;
        if (reason != null) { System.out.println(reason); exitCode = 1; }
        shutdown.countDown();
    }
    private void finish() {
        if (!finished.compareAndSet(false, true)) return;
        shutdownRequest(null);
        try { communicator.broadcast(create(GoOfflineDatagram::new)); }
        catch (IOException ex) { System.out.println("Could not send goodbye: " + ex.getMessage()); }
        communicator.close();
    }
    private void addPeer(Datagram datagram) {
        List<Peer> losers = peers.addOrUpdate(new Peer(datagram.senderId, datagram.name,
                datagram.remoteEndPoint, OffsetDateTime.parse(datagram.registeredAt).toInstant(), Instant.now()));
        for (Peer loser : losers) {
            if (loser.id().equals(datagram.senderId)) logDrop(datagram, "Participant lost a name conflict.");
            if (loser.id().equals(senderId))
                shutdownRequest("Name '" + name + "' is occupied by an earlier registration. Shutting down.");
        }
    }
    private void keyboardLoop() {
        BufferedReader input = new BufferedReader(new InputStreamReader(System.in));
        try {
            while (!stopping.get()) {
                String line = input.readLine();
                if (line == null || line.equals("/quit")) { shutdownRequest(null); return; }
                if (stopping.get()) return;
                if (line.isBlank()) continue;
                if (line.equals("/users")) {
                    peers.getAll().stream().sorted(Comparator.comparing(Peer::name))
                            .forEach(peer -> System.out.println("* " + peer.name() + " (" + peer.endPoint() + ")"));
                    communicator.broadcast(create(NameRequestDatagram::new));
                    continue;
                }
                if (line.startsWith("/msg ")) {
                    String[] parts = line.split(" +", 3);
                    if (parts.length != 3) { System.out.println("Usage: /msg name text"); continue; }
                    if (parts[2].length() > 500) { System.out.println("A message may contain at most 500 characters."); continue; }
                    Peer receiver = peers.find(parts[1]);
                    if (receiver == null) { System.out.println("User '" + parts[1] + "' is not online."); continue; }
                    PrivateMessageDatagram message = create(PrivateMessageDatagram::new);
                    message.encrypt(parts[2], key);
                    communicator.sendDatagram(message, receiver.endPoint());
                    System.out.println("[private to " + receiver.name() + "] " + name + ": " + parts[2]);
                    continue;
                }
                if (line.startsWith("/")) { System.out.println("Commands: /msg name text, /users, /quit."); continue; }
                if (line.length() > 500) { System.out.println("A message may contain at most 500 characters."); continue; }
                PublicMessageDatagram message = create(PublicMessageDatagram::new);
                message.message = line;
                communicator.broadcast(message);
                System.out.println("[general] " + name + ": " + line);
            }
        } catch (IOException | GeneralSecurityException ex) {
            if (!stopping.get()) shutdownRequest("Keyboard/send error: " + ex.getMessage());
        }
    }
    private void receiveLoop() {
        try {
            while (!stopping.get()) {
                Datagram datagram = communicator.receiveOne();
                if (datagram.senderId.equals(senderId)) { logDrop(datagram, "Own datagram."); continue; }
                peers.updateLastSeen(datagram.senderId, Instant.now());
                if (datagram instanceof NameRequestDatagram)
                    communicator.sendDatagram(create(NameResponseDatagram::new), datagram.remoteEndPoint);
                else if (datagram instanceof NameResponseDatagram) addPeer(datagram);
                else if (datagram instanceof RegistrationRequestDatagram) {
                    boolean isNew = peers.find(datagram.senderId) == null;
                    addPeer(datagram);
                    if (isNew && peers.find(datagram.senderId) != null)
                        System.out.println("* " + datagram.name + " joined the chat");
                } else if (datagram instanceof GoOfflineDatagram) {
                    Peer departed = peers.remove(datagram.senderId);
                    if (departed != null) System.out.println("* " + departed.name() + " left the chat");
                } else if (datagram instanceof PublicMessageDatagram message)
                    System.out.println("[general] " + message.name + ": " + message.message);
                else if (datagram instanceof PrivateMessageDatagram message) {
                    try {
                        String plaintext = message.decrypt(key);
                        if (plaintext.length() <= 500) System.out.println("[private] " + message.name + ": " + plaintext);
                        else {
                            logDrop(message, "Private message exceeds 500 characters.");
                            System.out.println("Dropped private message longer than 500 characters.");
                        }
                    } catch (GeneralSecurityException | IllegalArgumentException ex) {
                        logDrop(message, "Private message authentication or decoding failed.");
                        System.out.println("Cannot decrypt private message from " + message.name + ".");
                    }
                }
            }
        } catch (IOException ex) {
            if (!stopping.get()) shutdownRequest("Receive error: " + ex.getMessage());
        }
    }
    private void heartbeatLoop() {
        try {
            while (!shutdown.await(5, TimeUnit.SECONDS)) {
                communicator.broadcast(create(IAmStillHereDatagram::new));
                Instant now = Instant.now();
                peers.updateLastSeen(senderId, now);
                for (Peer peer : peers.removeExpired(now, Duration.ofSeconds(15)))
                    System.out.println("* " + peer.name() + " left the chat (timeout)");
            }
        } catch (IOException ex) {
            if (!stopping.get()) shutdownRequest("Heartbeat error: " + ex.getMessage());
        } catch (InterruptedException ex) { Thread.currentThread().interrupt(); }
    }
}
