using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using LanChat.Network;
using LanChat.Network.Datagrams;
using LanChat.Network.Peers;

try
{
    Run(args);
}
catch (SocketException exception)
{
    Console.WriteLine($"Network error: {exception.Message}");
    Environment.ExitCode = 1;
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"File error: {exception.Message}");
    Environment.ExitCode = 1;
}

static void Run(string[] args)
{
    if (args.Length != 3 || string.IsNullOrWhiteSpace(args[0])
        || !int.TryParse(args[1], out int port) || port < 1024 || port > 65535)
    {
        Console.WriteLine("Usage: dotnet run -- <name> <port:1024-65535> \"<passphrase>\"");
        Environment.ExitCode = 1;
        return;
    }

    string name = args[0];
    Guid senderId = Guid.NewGuid();
    using DatagramLogger logger = new(Path.Combine("logs", $"chat-{port}-{senderId:N}.log"));
    Console.WriteLine($"Log: {logger.FilePath}");
    DateTimeOffset registeredAt = default;
    byte[] key = PrivateMessageDatagram.DeriveKey(args[2]);
    PeerRegistry peerRegistry = new();
    using CancellationTokenSource shutdown = new();
    using DatagramCommunicator communicator = new(name, port, logger);
    int shutdownRequested = 0;

    // Discovery is completed synchronously before any background Task starts.
    communicator.Broadcast(Create<NameRequestDatagram>());
    List<Datagram> discovered = communicator.ReceiveFor(TimeSpan.FromSeconds(1));
    foreach (Datagram datagram in discovered)
    {
        if (datagram.SenderId == senderId)
        {
            LogDrop(datagram, "Own datagram.");
            continue;
        }
        if (datagram is not (NameResponseDatagram or RegistrationRequestDatagram))
        {
            if (datagram is not NameRequestDatagram)
                LogDrop(datagram, "Ignored during discovery.");
            continue;
        }
        if (datagram.Name == name)
        {
            Console.WriteLine($"Name '{name}' is occupied.");
            Environment.ExitCode = 1;
            return;
        }
        AddPeer(datagram);
    }

    registeredAt = DateTimeOffset.UtcNow;
    peerRegistry.AddOrUpdate(new Peer(senderId, name,
        new IPEndPoint(IPAddress.Loopback, communicator.PrivatePort), registeredAt, registeredAt));
    communicator.Broadcast(Create<RegistrationRequestDatagram>());
    foreach (NameRequestDatagram request in discovered.OfType<NameRequestDatagram>())
    {
        if (request.SenderId != senderId)
            communicator.SendDatagram(Create<NameResponseDatagram>(), request.RemoteEndPoint!);
    }

    Console.WriteLine($"Joined as {name}. Commands: /msg name text, /users, /quit.");
    ConsoleCancelEventHandler onCancel = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        ShutdownRequest();
    };
    Console.CancelKeyPress += onCancel;
    try
    {
        _ = Task.Run(KeyboardLoop);
        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(HeartbeatLoop);
        shutdown.Token.WaitHandle.WaitOne();
    }
    finally
    {
        shutdown.Cancel();
        Console.CancelKeyPress -= onCancel;
        try { communicator.Broadcast(Create<GoOfflineDatagram>()); }
        catch (SocketException exception) { Console.WriteLine($"Could not send goodbye: {exception.Message}"); }
        // Dispose closes both sockets. ReceiveLoop also checks the shutdown token.
        communicator.Dispose();
    }

    T Create<T>() where T : Datagram, new() => new()
    {
        Name = name,
        SenderId = senderId,
        RegisteredAt = registeredAt
    };

    void LogDrop(Datagram datagram, string reason) =>
        logger.Dropped(datagram.RemoteEndPoint, datagram.Type, datagram.ReceivedSize, reason);

    void ShutdownRequest(string? reason = null)
    {
        if (Interlocked.Exchange(ref shutdownRequested, 1) != 0)
            return;
        if (reason is not null)
        {
            Console.WriteLine(reason);
            Environment.ExitCode = 1;
        }
        shutdown.Cancel();
    }

    void AddPeer(Datagram datagram)
    {
        List<Peer> losers = peerRegistry.AddOrUpdate(new Peer(
            datagram.SenderId, datagram.Name, datagram.RemoteEndPoint!,
            datagram.RegisteredAt, DateTimeOffset.UtcNow));
        if (losers.Any(peer => peer.Id == datagram.SenderId))
            LogDrop(datagram, "Participant lost a name conflict.");
        if (losers.Any(peer => peer.Id == senderId))
            ShutdownRequest($"Name '{name}' is occupied by an earlier registration. Shutting down.");
    }

    void KeyboardLoop()
    {
        try
        {
            while (!shutdown.IsCancellationRequested)
            {
                string? line = Console.ReadLine();
                if (line is null || line == "/quit")
                {
                    ShutdownRequest();
                    return;
                }
                if (shutdown.IsCancellationRequested)
                    return;
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (line == "/users")
                {
                    foreach (Peer peer in peerRegistry.GetAll().OrderBy(peer => peer.Name, StringComparer.Ordinal))
                        Console.WriteLine($"* {peer.Name} ({peer.EndPoint})");
                    communicator.Broadcast(Create<NameRequestDatagram>());
                    continue;
                }
                if (line.StartsWith("/msg ", StringComparison.Ordinal))
                {
                    string[] parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 3)
                    {
                        Console.WriteLine("Usage: /msg name text");
                        continue;
                    }
                    if (parts[2].Length > 500)
                    {
                        Console.WriteLine("A message may contain at most 500 characters.");
                        continue;
                    }
                    Peer? receiver = peerRegistry.Find(parts[1]);
                    if (receiver is null)
                    {
                        Console.WriteLine($"User '{parts[1]}' is not online.");
                        continue;
                    }
                    PrivateMessageDatagram message = Create<PrivateMessageDatagram>();
                    message.Encrypt(parts[2], key);
                    communicator.SendDatagram(message, receiver.EndPoint);
                    Console.WriteLine($"[private to {receiver.Name}] {name}: {parts[2]}");
                    continue;
                }
                if (line.StartsWith('/'))
                {
                    Console.WriteLine("Commands: /msg name text, /users, /quit.");
                    continue;
                }
                if (line.Length > 500)
                {
                    Console.WriteLine("A message may contain at most 500 characters.");
                    continue;
                }
                PublicMessageDatagram publicMessage = Create<PublicMessageDatagram>();
                publicMessage.Message = line;
                communicator.Broadcast(publicMessage);
                Console.WriteLine($"[general] {name}: {line}");
            }
        }
        catch (Exception exception) when (exception is SocketException or ObjectDisposedException or IOException)
        {
            if (!shutdown.IsCancellationRequested)
                ShutdownRequest($"Keyboard/send error: {exception.Message}");
        }
    }

    void ReceiveLoop()
    {
        try
        {
            while (!shutdown.IsCancellationRequested)
            {
                Datagram datagram = communicator.ReceiveOne(shutdown.Token);
                if (datagram.SenderId == senderId)
                {
                    LogDrop(datagram, "Own datagram.");
                    continue;
                }
                peerRegistry.UpdateLastSeen(datagram.SenderId, DateTimeOffset.UtcNow);
                switch (datagram)
                {
                    case NameRequestDatagram:
                        communicator.SendDatagram(Create<NameResponseDatagram>(), datagram.RemoteEndPoint!);
                        break;
                    case NameResponseDatagram:
                        AddPeer(datagram);
                        break;
                    case RegistrationRequestDatagram:
                        bool isNew = peerRegistry.Find(datagram.SenderId) is null;
                        AddPeer(datagram);
                        if (isNew && peerRegistry.Find(datagram.SenderId) is not null)
                            Console.WriteLine($"* {datagram.Name} joined the chat");
                        break;
                    case GoOfflineDatagram:
                        if (peerRegistry.Remove(datagram.SenderId) is Peer departed)
                            Console.WriteLine($"* {departed.Name} left the chat");
                        break;
                    case IAmStillHereDatagram:
                        break; // LastSeen was updated above.
                    case PublicMessageDatagram message:
                        Console.WriteLine($"[general] {message.Name}: {message.Message}");
                        break;
                    case PrivateMessageDatagram message:
                        try
                        {
                            string plaintext = message.Decrypt(key);
                            if (plaintext.Length <= 500)
                                Console.WriteLine($"[private] {message.Name}: {plaintext}");
                            else
                            {
                                LogDrop(message, "Private message exceeds 500 characters.");
                                Console.WriteLine("Dropped private message longer than 500 characters.");
                            }
                        }
                        catch (Exception exception) when (exception is CryptographicException or FormatException)
                        {
                            LogDrop(message, "Private message authentication or decoding failed.");
                            Console.WriteLine($"Cannot decrypt private message from {message.Name}.");
                        }
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
        catch (Exception exception) when (exception is SocketException or ObjectDisposedException)
        {
            if (!shutdown.IsCancellationRequested)
                ShutdownRequest($"Receive error: {exception.Message}");
        }
    }

    void HeartbeatLoop()
    {
        try
        {
            while (!shutdown.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
            {
                communicator.Broadcast(Create<IAmStillHereDatagram>());
                DateTimeOffset now = DateTimeOffset.UtcNow;
                peerRegistry.UpdateLastSeen(senderId, now);
                foreach (Peer peer in peerRegistry.RemoveExpired(now, TimeSpan.FromSeconds(15)))
                    Console.WriteLine($"* {peer.Name} left the chat (timeout)");
            }
        }
        catch (Exception exception) when (exception is SocketException or ObjectDisposedException)
        {
            if (!shutdown.IsCancellationRequested)
                ShutdownRequest($"Heartbeat error: {exception.Message}");
        }
    }
}
