using System.Net;
using System.Net.Sockets;
using System.Text;
using LanChat.Network.Datagrams;

namespace LanChat.Network;

public abstract class DatagramCommunicator
{
    public string Name { get; set; }
    public int Port { get; set; }

    private Socket socket;
    private IPEndPoint broadcastEndPoint;

    public DatagramCommunicator(string name, int port)
    {
        Name = name;
        Port = port;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        socket.ReceiveTimeout = 1000;

        broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
    }

    protected void sendBroadcast(byte[] data)
    {
        socket.SendTo(data, broadcastEndPoint);
    }

    protected byte[] receiveBroadcast()
    {
        var buffer = new byte[6000];
        var receivedBytes = socket.Receive(buffer);
        return buffer[..receivedBytes];
    }

    public virtual void Send() { }

    public virtual Datagram Receive()
    {
        throw new NotImplementedException();
    }
}
