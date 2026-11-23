using System.Net;
using System.Net.Sockets;
using System.Text;
using LanChat.Network.Datagrams;

namespace LanChat.Network;

public class DatagramSender
{
    private readonly Socket _socket;
    private readonly IPEndPoint _broadcastEndPoint;
    private readonly DatagramLogger? _logger;

    public DatagramSender(Socket socket, IPEndPoint broadcastEndPoint, DatagramLogger? logger = null)
    {
        _socket = socket;
        _broadcastEndPoint = broadcastEndPoint;
        _logger = logger;
    }

    public void SendDatagram(Datagram datagram)
    {
        SendDatagram(datagram, _broadcastEndPoint);
    }

    public void SendDatagram(Datagram datagram, IPEndPoint destination)
    {
        string jsonMessage = datagram.ToJson();
        byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
        try
        {
            int count = _socket.SendTo(messageBytes, destination);
            _logger?.Sent(destination, datagram.Type, count);
        }
        catch (Exception exception) when (exception is SocketException or ObjectDisposedException)
        {
            _logger?.SendFailed(destination, datagram.Type, messageBytes.Length, exception.Message);
            throw;
        }
    }
}
