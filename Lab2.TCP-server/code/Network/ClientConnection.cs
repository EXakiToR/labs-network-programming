using System.Net.Sockets;
using System.Text;
using CoffeeShop.Handlers;
using CoffeeShop.Requests;
using Newtonsoft.Json;

namespace CoffeeShop.Network;

public sealed class ClientConnection
{
    private const int MaxMessageSize = 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    private readonly Socket _socket;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public ClientConnection(Socket socket)
    {
        _socket = socket;
    }

    public void Cancel() => _cts.Cancel();

    public async Task HandleAsync()
    {
        try
        {
            await ReadRequestsAsync(_cts.Token);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            Server.Instance.RemoveClientConnection(this);
            Cancel();
            _socket.Dispose();
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
                byte value = buffer[i];
                if (value == (byte)'\n')
                {
                    if (!discardUntilNewline && message.Count > 0)
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
                    Console.WriteLine("Message exceeds the 1 MiB limit");
                    continue;
                }

                message.Add(value);
            }
        }
    }

    private async Task DispatchMessageAsync(List<byte> message)
    {
        RequestEnvelope? request;
        try
        {
            string json = Utf8.GetString(message.ToArray());
            if (string.IsNullOrWhiteSpace(json))
                return;

            request = RequestEnvelope.Parse(json);
        }
        catch (DecoderFallbackException)
        {
            Console.WriteLine("Invalid UTF-8 in request");
            return;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing JSON: {ex.Message}");
            return;
        }

        if (request is null)
        {
            Console.WriteLine("Invalid request structure");
            return;
        }

        await RequestDispatcher.DispatchAsync(request, this);
    }
}
