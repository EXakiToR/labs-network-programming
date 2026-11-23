package LanChat.Network;
import java.io.IOException;
import java.net.*;
import java.nio.charset.StandardCharsets;
import LanChat.Network.Datagrams.Datagram;

public final class DatagramSender {
    private final DatagramSocket socket;
    private final InetSocketAddress broadcastEndPoint;
    private final DatagramLogger logger;
    public DatagramSender(DatagramSocket socket, InetSocketAddress broadcastEndPoint, DatagramLogger logger) {
        this.socket = socket;
        this.broadcastEndPoint = broadcastEndPoint;
        this.logger = logger;
    }
    public void sendDatagram(Datagram datagram) throws IOException { sendDatagram(datagram, broadcastEndPoint); }
    public void sendDatagram(Datagram datagram, InetSocketAddress destination) throws IOException {
        byte[] bytes = datagram.toJson().getBytes(StandardCharsets.UTF_8);
        try {
            socket.send(new DatagramPacket(bytes, bytes.length, destination));
            logger.write("SEND", destination, datagram.getType(), bytes.length, null);
        } catch (IOException ex) {
            logger.write("SEND_FAILED", destination, datagram.getType(), bytes.length, ex.getMessage());
            throw ex;
        }
    }
}
