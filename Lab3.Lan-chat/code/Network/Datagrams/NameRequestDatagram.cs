namespace LanChat.Network.Datagrams;

public class NameRequestDatagram : Datagram
{
    public NameRequestDatagram(string type, int port, string message)
        : base(type, port, message) { }
}
