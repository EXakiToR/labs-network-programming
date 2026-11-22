namespace LanChat.Network;

using System.Net;
using System.Net.Sockets;
using System.Text;
using LanChat.Network.Datagrams;

class NameRequestDatagramSender : DatagramCommunicator
{
    public NameRequestDatagramSender(string Name, int port)
        : base(Name, port) { }

    public override void Send()
    {
        var datagram = new NameRequestDatagram("NameRequest", Port, Name);

        var json = datagram.ToJson();
        var bytes = Encoding.UTF8.GetBytes(json);
        this.sendBroadcast(bytes);
    }
}
