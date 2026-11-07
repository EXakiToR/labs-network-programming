using System.Net;
using System.Net.Sockets;

// Client can:
// 1. Ask for the menu.
// 2. Order
//
Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 5050));
listener.Listen();
Socket client = listener.Accept();

byte[] buffer = new byte[1024];
while (true)
{
    int count = client.Receive(buffer);
    if (count == 0)
        break;
    client.Send(buffer, 0, count, SocketFlags.None);
}
client.Close();

Console.WriteLine("Hello, World!");

// for every connection use new Task, that will be managed by Threads from Thread Pool.
