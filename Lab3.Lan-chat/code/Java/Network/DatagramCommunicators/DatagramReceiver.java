package LanChat.Network;
import java.io.IOException;
import java.net.*;
import java.nio.*;
import java.nio.charset.*;
import java.time.*;
import java.util.*;
import LanChat.Network.Datagrams.*;

public final class DatagramReceiver {
    private final DatagramSocket[] sockets;
    private final int bufferSize;
    private final DatagramLogger logger;
    private int nextSocket;
    public DatagramReceiver(int bufferSize, DatagramLogger logger, DatagramSocket... sockets) throws SocketException {
        this.bufferSize = bufferSize;
        this.logger = logger;
        this.sockets = sockets.clone();
        for (DatagramSocket socket : sockets) socket.setSoTimeout(25);
    }
    public Datagram receiveOne() throws IOException {
        while (!Thread.currentThread().isInterrupted()) {
            Datagram datagram = receiveNext();
            if (datagram != null) return datagram;
        }
        throw new SocketException("Receiver interrupted.");
    }
    public List<Datagram> receiveFor(Duration duration) throws IOException {
        List<Datagram> result = new ArrayList<>();
        long end = System.nanoTime() + duration.toNanos();
        while (System.nanoTime() < end) {
            Datagram datagram = receiveNext();
            if (datagram != null) result.add(datagram);
        }
        return result;
    }
    private Datagram receiveNext() throws IOException {
        DatagramSocket socket = sockets[nextSocket];
        nextSocket = (nextSocket + 1) % sockets.length;
        DatagramPacket packet = new DatagramPacket(new byte[65535], 65535);
        String type = "Unknown";
        try {
            socket.receive(packet);
            String json = StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT)
                    .decode(ByteBuffer.wrap(packet.getData(), 0, packet.getLength())).toString();
            Map<String, Object> fields = Json.parse(json);
            if (fields.get("Type") instanceof String value) type = value;
            if (packet.getLength() > bufferSize) throw new IllegalArgumentException("Datagram exceeds the buffer size.");
            Datagram datagram = deserialize(fields, type);
            datagram.remoteEndPoint = (InetSocketAddress) packet.getSocketAddress();
            datagram.receivedSize = packet.getLength();
            logger.write("RECEIVE", packet.getSocketAddress(), type, packet.getLength(), null);
            return datagram;
        } catch (SocketTimeoutException ex) { return null; }
        catch (IllegalArgumentException | DateTimeException | CharacterCodingException ex) {
            logger.write("DROP", packet.getSocketAddress(), type, packet.getLength(), ex.getMessage());
            System.out.println("Dropped datagram from " + packet.getSocketAddress() + ": " + ex.getMessage());
            return null;
        }
    }
    private static String field(Map<String, Object> fields, String name) {
        if (!(fields.get(name) instanceof String value)) throw new IllegalArgumentException("Missing " + name);
        return value;
    }
    private static Datagram deserialize(Map<String, Object> fields, String type) {
        Datagram datagram = switch (type) {
            case "NameRequest" -> new NameRequestDatagram();
            case "NameResponse" -> new NameResponseDatagram();
            case "RegistrationRequest" -> new RegistrationRequestDatagram();
            case "GoOffline" -> new GoOfflineDatagram();
            case "IAmStillHere" -> new IAmStillHereDatagram();
            case "PublicMessage" -> new PublicMessageDatagram();
            case "PrivateMessage" -> new PrivateMessageDatagram();
            default -> throw new IllegalArgumentException("Unknown datagram type.");
        };
        String id = field(fields, "SenderId");
        if (!id.matches("[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}"))
            throw new IllegalArgumentException("Invalid sender ID.");
        datagram.senderId = UUID.fromString(id);
        datagram.name = field(fields, "Name");
        if (datagram.senderId.equals(new UUID(0, 0)) || datagram.name.isBlank())
            throw new IllegalArgumentException("Missing participant identity.");
        if (fields.containsKey("RegisteredAt")) datagram.registeredAt = field(fields, "RegisteredAt");
        Instant time = OffsetDateTime.parse(datagram.registeredAt).toInstant();
        if (!(datagram instanceof NameRequestDatagram) && time.equals(Instant.parse("0001-01-01T00:00:00Z")))
            throw new IllegalArgumentException("Missing registration time.");
        if (datagram instanceof MessageDatagram message) message.message = field(fields, "Message");
        if (datagram instanceof PublicMessageDatagram message && message.message.length() > 500)
            throw new IllegalArgumentException("Invalid public message length.");
        if (datagram instanceof PrivateMessageDatagram message) {
            message.nonce = field(fields, "Nonce");
            message.tag = field(fields, "Tag");
        }
        return datagram;
    }
}
