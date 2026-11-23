namespace LanChat.Network.Datagrams;

public abstract class MessageDatagram : Datagram
{
    public string Message { get; set; } = "";
}
