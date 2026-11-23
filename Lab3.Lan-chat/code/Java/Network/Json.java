package LanChat.Network;

import java.util.*;

// Small strict JSON codec for the flat protocol objects. Escaping matches System.Text.Json.
public final class Json {
    private final String text;
    private int position;
    private int depth;
    private Json(String text) { this.text = text; }

    public static Map<String, Object> parse(String text) {
        Json parser = new Json(text);
        Object value = parser.value();
        parser.space();
        if (parser.position != text.length() || !(value instanceof Map))
            throw new IllegalArgumentException("Expected a JSON object.");
        @SuppressWarnings("unchecked")
        Map<String, Object> result = (Map<String, Object>) value;
        return result;
    }

    public static String stringify(Map<String, Object> fields) {
        StringJoiner result = new StringJoiner(",", "{", "}");
        fields.forEach((key, value) -> result.add(quote(key) + ":" +
                (value == null ? "null" : value instanceof Number ? value.toString() : quote(value.toString()))));
        return result.toString();
    }

    private static String quote(String value) {
        StringBuilder result = new StringBuilder("\"");
        for (char ch : value.toCharArray()) {
            switch (ch) {
                case '\\' -> result.append("\\\\");
                case '\b' -> result.append("\\b");
                case '\t' -> result.append("\\t");
                case '\n' -> result.append("\\n");
                case '\f' -> result.append("\\f");
                case '\r' -> result.append("\\r");
                default -> {
                    if (ch < 32 || ch > 126 || "\"<>&' +`".indexOf(ch) >= 0 && ch != ' ')
                        result.append(String.format("\\u%04X", (int) ch));
                    else result.append(ch);
                }
            }
        }
        return result.append('"').toString();
    }

    private void space() {
        while (position < text.length() && " \t\r\n".indexOf(text.charAt(position)) >= 0) position++;
    }
    private boolean take(char ch) {
        space();
        if (position < text.length() && text.charAt(position) == ch) { position++; return true; }
        return false;
    }
    private void require(char ch) {
        if (!take(ch)) throw new IllegalArgumentException("Invalid JSON.");
    }
    private Object value() {
        if (++depth > 64) throw new IllegalArgumentException("JSON nesting is too deep.");
        try {
            return readValue();
        } finally {
            depth--;
        }
    }
    private Object readValue() {
        space();
        if (position >= text.length()) throw new IllegalArgumentException("Incomplete JSON.");
        char ch = text.charAt(position);
        if (ch == '"') return string();
        if (ch == '{') {
            position++;
            Map<String, Object> fields = new LinkedHashMap<>();
            if (take('}')) return fields;
            do {
                space();
                String key = string();
                require(':');
                fields.put(key, value());
            } while (take(','));
            require('}');
            return fields;
        }
        if (ch == '[') {
            position++;
            List<Object> items = new ArrayList<>();
            if (take(']')) return items;
            do { items.add(value()); } while (take(','));
            require(']');
            return items;
        }
        for (String literal : List.of("true", "false", "null")) {
            if (text.startsWith(literal, position)) {
                position += literal.length();
                return literal.equals("null") ? null : Boolean.valueOf(literal);
            }
        }
        int start = position;
        while (position < text.length() && "-+0123456789.eE".indexOf(text.charAt(position)) >= 0) position++;
        String number = text.substring(start, position);
        if (!number.matches("-?(0|[1-9][0-9]*)(\\.[0-9]+)?([eE][+-]?[0-9]+)?"))
            throw new IllegalArgumentException("Invalid JSON value.");
        return Double.valueOf(number);
    }
    private String string() {
        require('"');
        StringBuilder result = new StringBuilder();
        while (position < text.length()) {
            char ch = text.charAt(position++);
            if (ch == '"') {
                String value = result.toString();
                for (int i = 0; i < value.length(); i++) {
                    if (Character.isHighSurrogate(value.charAt(i))) {
                        if (++i >= value.length() || !Character.isLowSurrogate(value.charAt(i)))
                            throw new IllegalArgumentException("Invalid Unicode.");
                    } else if (Character.isLowSurrogate(value.charAt(i)))
                        throw new IllegalArgumentException("Invalid Unicode.");
                }
                return value;
            }
            if (ch < 32) throw new IllegalArgumentException("Invalid string.");
            if (ch == '\\') {
                if (position >= text.length()) throw new IllegalArgumentException("Incomplete escape.");
                ch = text.charAt(position++);
                switch (ch) {
                    case '"', '\\', '/' -> result.append(ch);
                    case 'b' -> result.append('\b');
                    case 'f' -> result.append('\f');
                    case 'n' -> result.append('\n');
                    case 'r' -> result.append('\r');
                    case 't' -> result.append('\t');
                    case 'u' -> {
                        if (position + 4 > text.length()) throw new IllegalArgumentException("Invalid escape.");
                        result.append((char) Integer.parseInt(text.substring(position, position + 4), 16));
                        position += 4;
                    }
                    default -> throw new IllegalArgumentException("Invalid escape.");
                }
            } else result.append(ch);
        }
        throw new IllegalArgumentException("Unterminated string.");
    }
}
