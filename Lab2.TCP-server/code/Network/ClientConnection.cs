using System.Net.Sockets;
using System.Text;
using CoffeeShop.Handlers;
using CoffeeShop.Requests;
using Newtonsoft.Json;

namespace CoffeeShop.Network;

public sealed class ClientConnection
{
    private const int MaxMessageSize = 1024 * 1024; // 1 MiB
    private static readonly UTF8Encoding Utf8 = new(false, true);

    private readonly Socket _socket;
    private readonly string _endpoint;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private volatile ClientRole _role;

    public ClientRole Role
    {
        get => _role;
        set => _role = value;
    }

    internal CancellationToken Disconnected => _cts.Token;
    internal Task Completion { get; private set; } = Task.CompletedTask;

    public ClientConnection(Socket socket)
    {
        _socket = socket;
        _endpoint = socket.RemoteEndPoint?.ToString() ?? "unknown endpoint";
    }

    internal void Start() => Completion = HandleAsync();

    internal void Log(string message) => Console.WriteLine($"[{_endpoint}] {message}");

    public void Cancel() => _cts.Cancel();

    public async Task HandleAsync()
    {
        try
        {
            Log("Connection accepted");
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            await ReadRequestsAsync(_cts.Token);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Log($"Connection error: {ex.Message}");
        }
        finally
        {
            Cancel();
            _socket.Dispose();
            Server.Instance.RemoveClientConnection(this);
            Log("Connection closed");
        }
    }

    public async Task SendMessageAsync(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.Contains('\n'))
            throw new ArgumentException("A message must fit on one line.", nameof(message));

        ReadOnlyMemory<byte> remaining = Utf8.GetBytes(message + "\n");
        var cancellationToken = _cts.Token;
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            while (!remaining.IsEmpty)
            {
                int sent = await _socket.SendAsync(remaining, SocketFlags.None, cancellationToken);
                if (sent == 0)
                    throw new IOException("The connection closed while sending a message.");
                remaining = remaining[sent..];
            }
        }
        catch
        {
            Cancel();
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReadRequestsAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        var message = new List<byte>();
        bool discardUntilNewline = false;

        while (true)
        {
            int count = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                byte value = buffer[i];
                if (value == (byte)'\n')
                {
                    if (!discardUntilNewline)
                        await DispatchMessageAsync(message);

                    message.Clear();
                    discardUntilNewline = false;
                    continue;
                }

                if (discardUntilNewline)
                    continue;
                if (message.Count == MaxMessageSize)
                {
                    message.Clear();
                    discardUntilNewline = true;
                    Log("Invalid request: message exceeds the 1 MiB limit");
                    await SendMessageAsync("Invalid request: message exceeds the 1 MiB limit");
                    continue;
                }

                message.Add(value);
            }
        }
    }

    private async Task DispatchMessageAsync(List<byte> message)
    {
        RequestEnvelope? request = null;
        try
        {
            request = RequestEnvelope.Parse(Utf8.GetString(message.ToArray()));
        }
        catch (Exception ex) when (ex is DecoderFallbackException or JsonException)
        {
            Log($"Invalid request: {ex.Message}");
        }

        if (request is null)
        {
            Log("Request refused: invalid JSON, command or parameters");
            await SendMessageAsync("Invalid request");
            return;
        }

        await SendMessageAsync($"Request of type {request.Type} received");
        await RequestDispatcher.DispatchAsync(request, this);
    }
}

public enum ClientRole
{
    Undefined,
    RegisteredUser,
    DeliveryPerson,
}
