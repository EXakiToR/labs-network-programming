using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LanChat.Network.Datagrams;

namespace LanChat.Network;

public sealed class DatagramReceiver
{
    private readonly Socket[] _sockets;
    private readonly int _bufferSize;
    private readonly DatagramLogger? _logger;
    private int _nextSocket;

    public DatagramReceiver(Socket socket, int bufferSize) : this(bufferSize, socket) { }

    public DatagramReceiver(int bufferSize, params Socket[] sockets) : this(bufferSize, null, sockets) { }

    public DatagramReceiver(int bufferSize, DatagramLogger? logger, params Socket[] sockets)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
        if (sockets.Length == 0)
            throw new ArgumentException("At least one socket is required.", nameof(sockets));
        _sockets = sockets.ToArray();
        _bufferSize = bufferSize;
        _logger = logger;
    }

    public Datagram ReceiveOne(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Socket? ready = WaitForSocket(cancellationToken.CanBeCanceled ? 100_000 : -1);
            if (ready is not null && ReceiveReady(ready) is Datagram datagram)
                return datagram;
        }
    }

    public List<Datagram> ReceiveFor(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        List<Datagram> datagrams = new();
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < duration)
        {
            TimeSpan remaining = duration - stopwatch.Elapsed;
            int microseconds = (int)Math.Clamp(Math.Ceiling(remaining.TotalMicroseconds), 0, int.MaxValue);
            Socket? ready = WaitForSocket(microseconds);
            if (ready is not null && ReceiveReady(ready) is Datagram datagram)
                datagrams.Add(datagram);
        }
        return datagrams;
    }

    private Socket? WaitForSocket(int microseconds)
    {
        List<Socket> ready = new();
        for (int i = 0; i < _sockets.Length; i++)
            ready.Add(_sockets[(_nextSocket + i) % _sockets.Length]);
        // No receive Task per socket: select readiness, then receive exactly one packet.
        Socket.Select(ready, null, null, microseconds);
        if (ready.Count == 0)
            return null;
        Socket socket = ready[0];
        _nextSocket = (Array.IndexOf(_sockets, socket) + 1) % _sockets.Length;
        return socket;
    }

    private Datagram? ReceiveReady(Socket socket)
    {
        // Read the whole UDP payload so an oversized packet's logged size is exact.
        byte[] buffer = new byte[ushort.MaxValue];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        int count = 0;
        string type = "Unknown";
        try
        {
            count = socket.ReceiveFrom(buffer, ref remote);
            if (count > _bufferSize)
            {
                type = ReadType(buffer.AsSpan(0, count));
                throw new JsonException("Datagram exceeds the buffer size.");
            }
            Datagram datagram = Deserialize(buffer.AsSpan(0, count), out type);
            datagram.RemoteEndPoint = (IPEndPoint)remote;
            datagram.ReceivedSize = count;
            _logger?.Received(remote, datagram.Type, count);
            return datagram;
        }
        catch (JsonException exception)
        {
            _logger?.Dropped(remote, type, count, exception.Message);
            Console.WriteLine($"Dropped datagram from {remote}: {exception.Message}");
            return null;
        }
        catch (SocketException exception) when (exception.SocketErrorCode == SocketError.MessageSize)
        {
            _logger?.Dropped(remote, type, null, "UDP payload was truncated by the socket.");
            Console.WriteLine($"Dropped oversized datagram from {remote}.");
            return null;
        }
    }

    private static string ReadType(ReadOnlySpan<byte> json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json.ToArray());
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("Type", out JsonElement kind)
                && kind.ValueKind == JsonValueKind.String ? kind.GetString()! : "Unknown";
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return "Unknown";
        }
    }

    private static Datagram Deserialize(ReadOnlySpan<byte> json, out string type)
    {
        type = "Unknown";
        try
        {
            return DeserializeJson(json, out type);
        }
        catch (InvalidOperationException exception)
        {
            // JsonDocument can accept an escaped surrogate that GetString cannot decode.
            // Normalize that parsing failure so ReceiveReady logs and drops the packet.
            throw new JsonException("Invalid Unicode or JSON value in datagram.", exception);
        }
    }

    private static Datagram DeserializeJson(ReadOnlySpan<byte> json, out string type)
    {
        type = "Unknown";
        using JsonDocument document = JsonDocument.Parse(json.ToArray());
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("Type", out JsonElement kind)
            || kind.ValueKind != JsonValueKind.String)
            throw new JsonException("Missing datagram type.");

        type = kind.GetString()!;
        Datagram? datagram = type switch
        {
            "NameRequest" => JsonSerializer.Deserialize<NameRequestDatagram>(json),
            "NameResponse" => JsonSerializer.Deserialize<NameResponseDatagram>(json),
            "RegistrationRequest" => JsonSerializer.Deserialize<RegistrationRequestDatagram>(json),
            "GoOffline" => JsonSerializer.Deserialize<GoOfflineDatagram>(json),
            "IAmStillHere" => JsonSerializer.Deserialize<IAmStillHereDatagram>(json),
            "PublicMessage" => JsonSerializer.Deserialize<PublicMessageDatagram>(json),
            "PrivateMessage" => JsonSerializer.Deserialize<PrivateMessageDatagram>(json),
            _ => throw new JsonException($"Unknown datagram type: {kind.GetString()}")
        };
        if (datagram is null || datagram.SenderId == Guid.Empty || string.IsNullOrWhiteSpace(datagram.Name))
            throw new JsonException("Missing participant identity.");
        if (datagram is not NameRequestDatagram && datagram.RegisteredAt == default)
            throw new JsonException("Missing registration time.");
        if (datagram is PublicMessageDatagram message && (message.Message is null || message.Message.Length > 500))
            throw new JsonException("Invalid public message length.");
        if (datagram is PrivateMessageDatagram privateMessage
            && (privateMessage.Message is null || privateMessage.Nonce is null || privateMessage.Tag is null))
            throw new JsonException("Missing encrypted message fields.");
        return datagram;
    }
}
