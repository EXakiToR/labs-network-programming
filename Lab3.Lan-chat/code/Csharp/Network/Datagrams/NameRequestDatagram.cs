namespace LanChat.Network.Datagrams;

public sealed class NameRequestDatagram : Datagram
{
    public override string Type => "NameRequest";
}
