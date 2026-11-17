# Coffee shop console client

Requires a JDK (Java 8 or newer). From the lab directory, start the .NET server
in one terminal:

```sh
dotnet run --project code/CoffeeShop.csproj -- 5050
```

In another terminal, compile and open a customer session:

```sh
cd ClientSide
javac CoffeeShopClient.java
java CoffeeShopClient localhost 5050 client
```

For a provider session, run this from `ClientSide` in another terminal:

```sh
java CoffeeShopClient localhost 5050 provider
```

- Customers can type `menu` or `order croissant 2 coffee 1`.
- Providers can type `delivery coffee 20 croissant 10`.
- Use `help` to see the commands for your role and `quit` to disconnect.
  Disconnecting cancels any unfinished orders on that connection.

Use the server machine's address in place of `localhost` for a remote session.
Each request is a UTF-8 JSON line. Replies and notifications appear as they
arrive, including while you are typing.
