using System.Net;
using System.Net.Sockets;
using System.Text;
using LanChat.Network.Datagrams;

namespace LanChat.Network;

public class NameRequestDatagramReciever : DatagramCommunicator
{
    public NameRequestDatagramReciever(string Name, int port)
        : base(Name, port) { }

    public override NameRequestDatagram Receive()
    {
        var bytes = this.receiveBroadcast();
        var json = Encoding.UTF8.GetString(bytes);
        var datagram = System.Text.Json.JsonSerializer.Deserialize<NameRequestDatagram>(json);
        if (datagram == null)
        {
            throw new Exception("NameRequestDatagramReciever: Returned datagram is null");
        }
        return datagram;
    }
}
