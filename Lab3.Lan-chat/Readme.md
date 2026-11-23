# Lab 3 - A LAN chat

Peer-to-peer UDP console chat in C# and Java. Both implementations use the same
JSON datagrams and can communicate, including encrypted private messages.
The Java implementation mirrors the C# datagram classes, communicator,
sender/receiver, peer registry and logger.

## Requirements

- C# requires the .NET 10 SDK.
- Java requires a JDK 17 or newer, with both `java` and `javac` on `PATH`.
  JDK 25 is supported. Check with `java -version` and `javac -version`.
- All peers use the same UDP chat port and passphrase, and different names.
- Allow the programs through the firewall on the private network. The network
  must allow UDP broadcast. A phone hotspot can be used for the lab demonstration.

The installed VS Code Java launcher on this Windows machine converted non-ASCII command-line characters during testing. Use ASCII names and passphrases with that launcher.

The commands below use PowerShell and start from the `Lab3.Lan-chat` directory.

## Build

Build C#:

```powershell
dotnet build .\code\Csharp\LanChat.csproj
```

Compile and run Java from the lab directory:

```powershell
cd .\code\Java
javac -encoding UTF-8 -d out Main.java Network\*.java Network\Datagrams\*.java Network\DatagramCommunicators\*.java Network\Peers\*.java
java "-Djava.net.preferIPv4Stack=true" -cp .\out LanChat.Main Ion 5050 "shared secret"
```

The Java packages use `LanChat` and `LanChat.Network`, matching the C#
namespaces. Compiling all source files together preserves the same logical
folder organization as C#.

## Run C# and Java together

In terminal 1, start the C# peer:

```powershell
dotnet run --project .\code\Csharp\LanChat.csproj -- Ana 5050 "shared secret"
```

In terminal 2, start the Java peer:

```powershell
java "-Djava.net.preferIPv4Stack=true" -cp .\code\Java\out LanChat.Main Ion 5050 "shared secret"
```

For a third peer, use another terminal or another computer on the same LAN:

```powershell
java "-Djava.net.preferIPv4Stack=true" -cp .\code\Java\out LanChat.Main Maria 5050 "shared secret"
```

Alternatively, start another C# peer:

```powershell
dotnet run --project .\code\Csharp\LanChat.csproj -- Maria 5050 "shared secret"
```

Use a port from 1024 to 65535. No server address is required. The Java IPv4
option matches the IPv4 sockets used by C#. Quote the entire -D option in PowerShell. From code/Java, use -cp .\out instead of -cp .\code\Java\out.

## Commands

| Input | Effect |
| --- | --- |
| `hello everyone` | Broadcast a general message |
| `/msg Ana see you at five` | Send an encrypted private message to Ana |
| `/users` | List online peers and request discovery replies |
| `/quit` | Broadcast goodbye and exit |

Messages contain at most 500 UTF-16 code units, matching C# string length.
Incoming messages appear while keyboard input is open. Discovery takes about
one second. Peers announce their presence every five seconds and expire after
15 seconds without traffic, checked on each heartbeat.

To demonstrate interoperability, type `hello from Java` in Ion's terminal and
`/msg Ion hello from Csharp` in Ana's terminal. Then reply with
`/msg Ana hello from Java`. Both peers display the decrypted private message.
Starting another peer named Ana reports that the name is occupied.

## Shared protocol

Each UDP datagram contains one UTF-8 JSON object, with case-sensitive fields:
`Type`, `SenderId` (UUID), `Name`, and `RegisteredAt` (ISO 8601 timestamp).
The types are `NameRequest`, `NameResponse`, `RegistrationRequest`,
`GoOffline`, `IAmStillHere`, `PublicMessage`, and `PrivateMessage`.
Message datagrams also carry `Message`. Private messages carry Base64-encoded
ciphertext in `Message`, a 12-byte `Nonce`, and a 16-byte authentication `Tag`.

Each peer binds one reusable socket to `0.0.0.0:<chat-port>` for broadcasts,
and a second socket to a unique ephemeral port for private messages and replies.
All sends originate from the second socket, so discovery learns the correct
private endpoint even when several peers share a computer.

Private messages use AES-256-GCM. PBKDF2-HMAC-SHA256 derives a 32-byte key from
the passphrase with 600,000 iterations and UTF-8 salt `LanChat/v1/shared-key`.
The authenticated metadata is the compact JSON object containing `Type`,
`SenderId`, `Name`, and `RegisteredAt`, in that order, with C#-compatible
escaping and timestamp formatting. General messages remain readable.

Each peer writes a separate JSON-lines log under `logs` in its working
directory. Logs contain datagram metadata and drop reasons. Incoming datagrams
over 6000 bytes, malformed JSON, invalid identities and unknown types are
dropped and logged.
