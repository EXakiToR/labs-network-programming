using System.Net;
using System.Text.Json;

namespace LanChat.Network;

public sealed class DatagramLogger : IDisposable
{
    public string FilePath { get; }

    private readonly TextWriter _writer;
    private int _disposed;

    public DatagramLogger(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        // Sends and receives can log from different Tasks; each line is written atomically.
        _writer = TextWriter.Synchronized(new StreamWriter(FilePath, append: true)
        {
            AutoFlush = true
        });
    }

    public void Sent(EndPoint endpoint, string type, int size) =>
        Write("SEND", endpoint, type, size);

    public void Received(EndPoint endpoint, string type, int size) =>
        Write("RECEIVE", endpoint, type, size);

    public void Dropped(EndPoint? endpoint, string type, int? size, string reason) =>
        Write("DROP", endpoint, type, size, reason);

    public void SendFailed(EndPoint endpoint, string type, int size, string reason) =>
        Write("SEND_FAILED", endpoint, type, size, reason);

    private void Write(string action, EndPoint? endpoint, string type, int? size, string? reason = null)
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;

        // Only metadata: message contents and passphrases never enter the log.
        string line = JsonSerializer.Serialize(new
        {
            Timestamp = DateTimeOffset.UtcNow,
            Action = action,
            Endpoint = endpoint?.ToString() ?? "unknown",
            Type = type,
            Size = size,
            Reason = reason
        });
        try
        {
            _writer.WriteLine(line);
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0) { }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Cannot write datagram log: {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            _writer.Dispose();
    }
}
