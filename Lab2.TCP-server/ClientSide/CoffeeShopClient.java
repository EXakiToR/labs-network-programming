import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.Locale;
import java.util.StringJoiner;

public final class CoffeeShopClient {
    private static final int TIMEOUT_MS = 5000;

    public static void main(String[] args) {
        if (args.length != 3 || (!args[2].equals("client") && !args[2].equals("provider"))) {
            System.err.println("Usage: java CoffeeShopClient <host> <port> <client|provider>");
            System.exit(1);
        }

        int port;
        try {
            port = Integer.parseInt(args[1]);
            if (port < 1 || port > 65535) {
                throw new NumberFormatException();
            }
        } catch (NumberFormatException ex) {
            System.err.println("Port must be an integer from 1 to 65535.");
            System.exit(1);
            return;
        }

        try {
            run(args[0], port, args[2]);
        } catch (IOException ex) {
            System.err.println("Connection error: " + ex.getMessage());
            System.exit(1);
        }
    }

    private static void run(String host, int port, String role) throws IOException {
        try (Socket socket = new Socket()) {
            socket.connect(new InetSocketAddress(host, port), TIMEOUT_MS);
            BufferedReader replies = new BufferedReader(
                    new InputStreamReader(socket.getInputStream(), StandardCharsets.UTF_8));
            BufferedWriter requests = new BufferedWriter(
                    new OutputStreamWriter(socket.getOutputStream(), StandardCharsets.UTF_8));

            socket.setSoTimeout(TIMEOUT_MS);
            send(requests, "register", "{\"Role\":\"" + role + "\"}");
            String registration = replies.readLine();
            if (registration == null) {
                throw new IOException("Server closed the connection during registration.");
            }
            System.out.println(registration);
            if (!registration.equals("Registered as " + role)) {
                throw new IOException("Server refused registration.");
            }
            socket.setSoTimeout(0);

            boolean provider = role.equals("provider");
            printHelp(provider);
            // A blocked console read must not keep the client alive after the server disconnects.
            Thread keyboard = new Thread(() -> readCommands(socket, requests, provider), "keyboard");
            keyboard.setDaemon(true);
            keyboard.start();

            try {
                String line;
                while ((line = replies.readLine()) != null) {
                    System.out.println(line);
                }
                if (!socket.isClosed()) {
                    System.out.println("Server disconnected.");
                }
            } catch (IOException ex) {
                if (!socket.isClosed()) {
                    System.out.println("Connection lost: " + ex.getMessage());
                }
            }
        }
    }

    private static void readCommands(Socket socket, BufferedWriter requests, boolean provider) {
        BufferedReader keyboard = new BufferedReader(new InputStreamReader(System.in));
        String itemsCommand = provider ? "delivery" : "order";
        try (Socket connection = socket) {
            String line;
            while ((line = keyboard.readLine()) != null && !connection.isClosed()) {
                String[] tokens = line.trim().toLowerCase(Locale.ROOT).split("\\s+");
                String command = tokens[0];
                if (tokens.length == 1) {
                    switch (command) {
                        case "quit":
                            return;
                        case "help":
                            printHelp(provider);
                            continue;
                        case "":
                            continue;
                        case "menu":
                            if (!provider) {
                                send(requests, "menu", "{}");
                                continue;
                            }
                            break;
                        default:
                            break;
                    }
                }
                if (command.equals(itemsCommand)) {
                    try {
                        send(requests, command, itemsPayload(tokens));
                    } catch (IllegalArgumentException ex) {
                        System.out.println(ex.getMessage());
                    }
                } else {
                    System.out.println("Unknown command or extra arguments. Type help.");
                }
            }
        } catch (IOException ex) {
            System.err.println("Could not read input or send request: " + ex.getMessage());
        }
    }

    private static String itemsPayload(String[] tokens) {
        if (tokens.length < 3 || tokens.length % 2 == 0) {
            throw new IllegalArgumentException("Enter one or more product and quantity pairs.");
        }

        StringJoiner items = new StringJoiner(",", "{\"Items\":[", "]}");
        for (int i = 1; i < tokens.length; i += 2) {
            String product = tokens[i];
            if (!product.equals("coffee") && !product.equals("tea") && !product.equals("croissant")) {
                throw new IllegalArgumentException("Unknown product. Choose coffee, tea or croissant.");
            }

            int quantity;
            try {
                quantity = Integer.parseInt(tokens[i + 1]);
                if (quantity < 1) {
                    throw new NumberFormatException();
                }
            } catch (NumberFormatException ex) {
                throw new IllegalArgumentException("Quantity must be an integer from 1 to 2147483647.");
            }
            items.add("{\"Product\":\"" + product + "\",\"Quantity\":" + quantity + "}");
        }
        return items.toString();
    }

    private static void send(BufferedWriter requests, String type, String payload) throws IOException {
        requests.write("{\"Type\":\"" + type + "\",\"Payload\":" + payload + "}\n");
        requests.flush();
    }

    private static void printHelp(boolean provider) {
        System.out.println(provider
                ? "Commands: delivery coffee 20 croissant 10 | help | quit"
                : "Commands: menu | order croissant 2 coffee 1 | help | quit");
    }
}
