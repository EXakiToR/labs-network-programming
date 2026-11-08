using System.Net;
using System.Net.Sockets;
using System.Text;
using CoffeeShop.Handlers;
using CoffeeShop.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Client can:
// 1. Ask for the menu.
// 2. Order
//

Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 5050));
listener.Listen();

while (true)
{
    // may do: create normal cancellatopn and error incapsulation.
    // not just that ugly try catch that can theoretically lead to infinite loop of errors.
    try
    {
        Socket connection = await listener.AcceptAsync();
        Console.WriteLine("Connection connected.");
        _ = TCPClientHandleAsync(connection);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static async Task TCPClientHandleAsync(Socket connection)
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
