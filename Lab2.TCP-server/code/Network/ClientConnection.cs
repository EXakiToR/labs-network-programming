namespace System.Net.Sockets;

using System.Text;
using CoffeeShop.Handlers;
using CoffeeShop.Network;
using CoffeeShop.Requests;
using Newtonsoft.Json;

public sealed class ClientConnection
{
    private const int MaxMessageSize = 1024 * 1024;

    private Socket _connection;
    private CancellationTokenSource _cts;

    public ClientConnection(Socket conn)
    {
        _connection = conn;
        _cts = new CancellationTokenSource();
    }

    public void Cancel()
    {
        _cts.Cancel();
    }

    public async Task HandleAsync()
    {
        var cancellationToken = _cts.Token;
        try
        {
            List<byte> JsonBytes = new();
            bool discardUntilNewline = false;
            byte[] buffer = new byte[1024];
            while (true)
            {
                int count = await _connection.ReceiveAsync(
                    buffer,
                    SocketFlags.None,
                    cancellationToken
                );
                if (count == 0)
                    break;

                for (int i = 0; i < count; i++)
                {
                    byte b = buffer[i];
                    if (discardUntilNewline)
                    {
                        if (b == (byte)'\n')
                            discardUntilNewline = false;

                        continue;
                    }

                    if (b == (byte)'\n')
                    {
                        string json = Encoding.UTF8.GetString(JsonBytes.ToArray());
                        JsonBytes.Clear();

                        if (String.IsNullOrWhiteSpace(json))
                        {
                            continue;
                        }
                        try
                        {
                            RequestEnvelope? request =
                                JsonConvert.DeserializeObject<RequestEnvelope>(json);
                            if (
                                request is null
                                || string.IsNullOrWhiteSpace(request.Type)
                                || request.Payload is null
                            )
                            {
                                Console.WriteLine("Invalid request structure");
                                continue;
                            }
                            // Here recieved valid JSON object of RequestEnvelope.
                            await RequestDispatcher.DispatchAsync(request, _connection); // to ? to use _ = ... or await.

                            Console.WriteLine($"Request Type: {request.Type}");
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"Error parsing JSON: {ex.Message}");
                        }
                    }
                    else
                    {
                        if (JsonBytes.Count >= MaxMessageSize)
                        {
                            JsonBytes.Clear();
                            discardUntilNewline = true;
                            Console.WriteLine("Message exceeds the 1 MiB limit");
                            continue;
                        }

                        JsonBytes.Add(b);
                    }
                }
                // recieved data, now, we can handle it
                //
                // manage JSON, read till '\n', that will be one complete JSON message.
                await _connection.SendAsync(buffer.AsMemory(0, count), SocketFlags.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleAssync: {ex.Message}");
        }
        finally
        {
            var server = Server.Instance;
            server.RemoveClientConnection(this);
            _connection.Close();
        }
    }
}
