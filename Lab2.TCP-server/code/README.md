# Coffee Shop TCP server

With the .NET 10 SDK installed, run these commands from this directory:

```sh
dotnet build CoffeeShop.csproj
dotnet run --project CoffeeShop.csproj -- 5050
```

The port defaults to `5050`. The server listens on all IPv4 interfaces. Press
Ctrl+C to stop the server and cancel unfinished orders. On the first run,
each product starts with 10–50 units. Later runs load `stock.json` from the
server's working directory.

## TCP protocol

Send one UTF-8 JSON request per line, terminated by LF. Register each
connection with either the `client` or the `provider` role.

```json
{"Type":"register","Payload":{"Role":"client"}}
{"Type":"menu","Payload":{}}
{"Type":"order","Payload":{"Items":[{"Product":"croissant","Quantity":2},{"Product":"coffee","Quantity":1}]}}
```

On a separate connection, the provider can send:

```json
{"Type":"register","Payload":{"Role":"provider"}}
{"Type":"delivery","Payload":{"Items":[{"Product":"coffee","Quantity":20}]}}
```

Replies are UTF-8 text lines. An accepted order reports its ID and total
preparation time in seconds. The ready message may arrive between replies to
other requests. Providers can receive low-stock alerts at any time.
