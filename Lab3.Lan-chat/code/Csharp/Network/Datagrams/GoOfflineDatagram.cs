namespace LanChat.Network.Datagrams;

public sealed class GoOfflineDatagram : Datagram
{
    public override string Type => "GoOffline";
}
