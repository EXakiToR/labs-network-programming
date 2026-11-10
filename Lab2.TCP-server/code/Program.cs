using System.Net;
using System.Net.Sockets;
using System.Text;
using CoffeeShop.Handlers;
using CoffeeShop.Network;
using CoffeeShop.Requests;
using Newtonsoft.Json;

// Client can:
// 1. Ask for the menu.
// 2. Order
//

// cancellation on ctrl+C
HashSet<Task> clientTasks = new();

using var cts = new CancellationTokenSource();

using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 5050));
listener.Listen();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// accept new connections.
while (!cts.IsCancellationRequested && ServerState.Status == ServerStatus.Accepting)
{
    // may do: create normal cancellatopn and error incapsulation.
    // not just that ugly try catch that can theoretically lead to infinite loop of errors.
    Socket connection;
    try
    {
        connection = await listener.AcceptAsync(cts.Token);
    }
    catch (OperationCanceledException) when (cts.IsCancellationRequested)
    {
        // cancellation requested, exit loop.
        Console.WriteLine("Server is shutting down, no longer accepting new connections.");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"FATAL server error: {ex.Message}\n Cannot more accept new connections, shutting down server."
        );
        // Notify all client handlers and stop accepting connections.
        cts.Cancel();
        break;
    }

    Task task = TCPClientHandleAsync(connection, cts.Token); // fire and keep eye on it, to handle multiple connections.
    // exception from a single client are incapsulated inside TCPClientHandleAsync
    clientTasks.Add(task);
    clientTasks.RemoveWhere(task =>
    {
        return task.IsCompleted;
    });
}
cts.Cancel();
try
{
    await Task.WhenAll(clientTasks).WaitAsync(TimeSpan.FromSeconds(5));
}
catch (TimeoutException)
{
    Console.WriteLine("Timeout waiting for clients to shutdown.");
}
catch (Exception ex)
{
    Console.WriteLine($"Clients shutdown error: {ex}");
}

//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
// handle separate connection.
static async Task TCPClientHandleAsync(Socket connection, CancellationToken listenerCts)
{
    try
    {
        List<byte> JsonBytes = new();
        byte[] buffer = new byte[1024];
        while (true)
        {
            int count = await connection.ReceiveAsync(buffer, SocketFlags.None);
            if (count == 0)
                break;

            for (int i = 0; i < count; i++)
            {
                byte b = buffer[i];
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
                        RequestEnvelope? request = JsonConvert.DeserializeObject<RequestEnvelope>(
                            json
                        );
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
                        await RequestDispatcher.DispatchAsync(request, connection); // to ? to use _ = ... or await.

                        Console.WriteLine($"Request Type: {request.Type}");
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Error parsing JSON: {ex.Message}");
                    }
                }
                else
                {
                    JsonBytes.Add(b);
                }
            }
            // recieved data, now, we can handle it
            //
            // manage JSON, read till '\n', that will be one complete JSON message.
            await connection.SendAsync(buffer.AsMemory(0, count), SocketFlags.None);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in HandleAssync: {ex.Message}");
    }
    finally
    {
        connection.Close();
    }
}

// for every connection use new Task, that will be managed by Threads from Thread Pool.
