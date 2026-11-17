using CoffeeShop.Core;
using CoffeeShop.Network;

int port = 5050;
if (args.Length > 1 || (args.Length == 1 && (!int.TryParse(args[0], out port) || port is < 1 or > 65535)))
{
    Console.Error.WriteLine("Usage: CoffeeShop [port] (1-65535)");
    return 1;
}

try
{
    Stock.Load();
    await Server.Instance.Run(port);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Server error: {ex.Message}");
    return 1;
}
