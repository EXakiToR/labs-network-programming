using System.Net;
using System.Net.Sockets;
using LanChat.Network.Datagrams;

namespace LanChat.Network;

public sealed class DatagramCommunicator : IDisposable
{
    public string Name { get; }
    public int Port { get; }
    public int PrivatePort => ((IPEndPoint)_privateSocket.LocalEndPoint!).Port;

    private readonly Socket _broadcastSocket;
    private readonly Socket _privateSocket;
    private readonly DatagramReceiver _datagramReceiver;
    private readonly DatagramSender _datagramSender;
    private const int BufferSize = 6000;

    public DatagramCommunicator(string name, int port, DatagramLogger? logger = null)
    {
        Name = name;
        Port = port;
        _broadcastSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _privateSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        try
        {
            _broadcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _broadcastSocket.Bind(new IPEndPoint(IPAddress.Any, port));

            // Broadcast and unicast both originate from this unique source port.
            _privateSocket.EnableBroadcast = true;
            _privateSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

            _datagramReceiver = new DatagramReceiver(BufferSize, logger, _broadcastSocket, _privateSocket);
            _datagramSender = new DatagramSender(_privateSocket, new IPEndPoint(IPAddress.Broadcast, port), logger);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void SendDatagram(Datagram datagram, IPEndPoint destination) =>
        _datagramSender.SendDatagram(datagram, destination);

    public void Broadcast(Datagram datagram) => _datagramSender.SendDatagram(datagram);

    // ReceiveFor finishes before Main starts its sole background receive loop.
    public Datagram ReceiveOne(CancellationToken cancellationToken = default) =>
        _datagramReceiver.ReceiveOne(cancellationToken);

    public List<Datagram> ReceiveFor(TimeSpan duration) => _datagramReceiver.ReceiveFor(duration);

    public void Dispose()
    {
        _broadcastSocket.Dispose();
        _privateSocket.Dispose();
    }
}
