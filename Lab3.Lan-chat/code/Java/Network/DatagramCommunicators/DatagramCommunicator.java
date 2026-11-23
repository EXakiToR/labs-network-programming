package LanChat.Network;
import java.io.IOException;
import java.net.*;
import java.time.Duration;
import java.util.List;
import LanChat.Network.Datagrams.Datagram;

public final class DatagramCommunicator implements AutoCloseable {
    public final String name;
    public final int port;
    private final DatagramSocket broadcastSocket;
    private final DatagramSocket privateSocket;
    private final DatagramReceiver receiver;
    private final DatagramSender sender;
    public DatagramCommunicator(String name, int port, DatagramLogger logger) throws IOException {
        this.name = name;
        this.port = port;
        broadcastSocket = new DatagramSocket(null);
        privateSocket = new DatagramSocket(null);
        try {
            broadcastSocket.setReuseAddress(true);
            broadcastSocket.bind(new InetSocketAddress("0.0.0.0", port));
            privateSocket.setBroadcast(true);
            privateSocket.bind(new InetSocketAddress("0.0.0.0", 0));
            receiver = new DatagramReceiver(6000, logger, broadcastSocket, privateSocket);
            sender = new DatagramSender(privateSocket, new InetSocketAddress("255.255.255.255", port), logger);
        } catch (IOException ex) { close(); throw ex; }
    }
    public int getPrivatePort() { return privateSocket.getLocalPort(); }
    public void broadcast(Datagram datagram) throws IOException { sender.sendDatagram(datagram); }
    public void sendDatagram(Datagram datagram, InetSocketAddress destination) throws IOException {
        sender.sendDatagram(datagram, destination);
    }
    public Datagram receiveOne() throws IOException { return receiver.receiveOne(); }
    public List<Datagram> receiveFor(Duration duration) throws IOException { return receiver.receiveFor(duration); }
    public void close() { broadcastSocket.close(); privateSocket.close(); }
}
