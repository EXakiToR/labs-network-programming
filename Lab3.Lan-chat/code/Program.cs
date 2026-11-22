using LanChat.Network;
using LanChat.Network.Datagrams;

if (args.Length != 3)
{
    Console.WriteLine($"Usage: dotnet run <name> <port> \"<passphrase>\"");
    return;
}

string name = args[0];
int port = int.Parse(args[1]);
if (port < 1024 || port > 65535)
{
    Console.WriteLine("Port must be between 1024 and 65535.");
    return;
}

string passphrase = args[2];

Console.WriteLine($"Name: {name}");
Console.WriteLine($"Port: {port}");
Console.WriteLine($"Passphrase: {passphrase}");

var NameRequestSender = new NameRequestDatagramSender(name, port);

NameRequestSender.Send();

// within one second collect responses and put in peer registry
var NameRequestReceiver = new NameRequestDatagramReceiver(name, port);
