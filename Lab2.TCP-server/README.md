# Coffee Shop (Lab 02)

In this laboratory work, we will develop a server using TCP sockets
in C#. Purpose of this lab is to understand how to create a TCP server
that can handle multiple clients simultaneously. The server will
simulate a coffee shop where clients can place orders and receive responses.

Possible:

```json
{
  "coffee": 45;
  "tea": 454
  "crossaint": 45
}
  ```

[12, 54, 65]

but we use
{
  {
  "type": "cofee",
  "quantity": 54
  },

  {
  "type": "cofee",
  "quantity": 54
  },

}

ServerState.cs - Responsible for maintaining the state of the server, including its current status and a cancellation token source for graceful shutdown.

Program.cs - Main entry point of the application. It sets up the TCP listener, handles incoming client connections, and manages server shutdown.

Program.cs only does that the ServeState.cs tells. It doesn't handle logic of changing state of the server, but, depending on its state, it does diffirent things.

So, ServerState.cs - the state.
What The server does in that state - Program.cs
