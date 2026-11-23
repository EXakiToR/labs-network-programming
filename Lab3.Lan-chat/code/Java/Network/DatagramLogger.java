package LanChat.Network;
import java.io.*;
import java.net.SocketAddress;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.time.Instant;
import java.util.*;

public final class DatagramLogger implements AutoCloseable {
    public final Path filePath;
    private final BufferedWriter writer;
    private boolean closed;
    public DatagramLogger(Path path) throws IOException {
        filePath = path.toAbsolutePath();
        Files.createDirectories(filePath.getParent());
        writer = Files.newBufferedWriter(filePath, StandardCharsets.UTF_8,
                StandardOpenOption.CREATE, StandardOpenOption.APPEND);
    }
    public synchronized void write(String action, SocketAddress endpoint, String type, Integer size, String reason) {
        if (closed) return;
        Map<String, Object> fields = new LinkedHashMap<>();
        fields.put("Timestamp", Instant.now());
        fields.put("Action", action);
        fields.put("Endpoint", endpoint == null ? "unknown" : endpoint.toString());
        fields.put("Type", type);
        fields.put("Size", size);
        fields.put("Reason", reason);
        try {
            writer.write(Json.stringify(fields));
            writer.newLine();
            writer.flush();
        } catch (IOException ex) { System.err.println("Cannot write datagram log: " + ex.getMessage()); }
    }
    public synchronized void close() throws IOException {
        if (!closed) { closed = true; writer.close(); }
    }
}
