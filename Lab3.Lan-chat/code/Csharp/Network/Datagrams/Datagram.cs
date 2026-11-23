using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LanChat.Network.Datagrams;

public abstract class Datagram
{
    public abstract string Type { get; }
    public Guid SenderId { get; init; }
    public string Name { get; init; } = "";
    public DateTimeOffset RegisteredAt { get; init; }

    // Transport metadata, never a field in the JSON message.
    [JsonIgnore]
    public IPEndPoint? RemoteEndPoint { get; internal set; }

    [JsonIgnore]
    public int ReceivedSize { get; internal set; }

    public string ToJson() => JsonSerializer.Serialize(this, GetType());
}
