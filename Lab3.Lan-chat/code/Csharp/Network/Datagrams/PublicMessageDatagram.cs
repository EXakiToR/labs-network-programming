namespace LanChat.Network.Datagrams;

public sealed class PublicMessageDatagram : MessageDatagram
{
    public override string Type => "PublicMessage";
}
