package LanChat.Network.Datagrams;

import java.net.InetSocketAddress;
import java.util.*;
import LanChat.Network.Json;

public abstract class Datagram {
    public UUID senderId;
    public String name = "";
    // Preserve the wire spelling for AES-GCM associated data.
    public String registeredAt = "0001-01-01T00:00:00+00:00";
    public InetSocketAddress remoteEndPoint;
    public int receivedSize;
    public abstract String getType();

    public Map<String, Object> identity() {
        Map<String, Object> fields = new LinkedHashMap<>();
        fields.put("Type", getType());
        fields.put("SenderId", senderId.toString());
        fields.put("Name", name);
        fields.put("RegisteredAt", registeredAt);
        return fields;
    }

    public String toJson() {
        Map<String, Object> fields = identity();
        if (this instanceof MessageDatagram message) fields.put("Message", message.message);
        if (this instanceof PrivateMessageDatagram message) {
            fields.put("Nonce", message.nonce);
            fields.put("Tag", message.tag);
        }
        return Json.stringify(fields);
    }
}
