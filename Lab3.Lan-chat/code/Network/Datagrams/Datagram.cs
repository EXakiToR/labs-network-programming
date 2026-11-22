namespace LanChat.Network.Datagrams;

public abstract class Datagram
{
    public string Type { get; set; }
    public int Port { get; set; }
    public string Message { get; set; }

    public Datagram(string type, int port, string message)
    {
        Type = type;
        Port = port;
        Message = message;
    }

    public virtual string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}
