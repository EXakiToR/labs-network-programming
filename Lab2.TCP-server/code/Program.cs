using CoffeeShop.Core;
using CoffeeShop.Network;

// Client can:
// 1. Ask for the menu.
// 2. Order
//
//
Stock.Load();

var server = Server.Instance;
await server.Run();
