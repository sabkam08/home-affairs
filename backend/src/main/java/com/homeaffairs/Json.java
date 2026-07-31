package com.homeaffairs;

import java.lang.reflect.Array;
import java.lang.reflect.Field;
import java.lang.reflect.Modifier;
import java.time.Instant;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public final class Json {

    private Json() {
    }

    public static String stringify(Object value) {
        StringBuilder builder = new StringBuilder();
        writeValue(builder, value);
        return builder.toString();
    }

    public static Map<String, Object> parseObject(String json) {
        Object parsed = new Parser(json).parseValue();
        if (parsed instanceof Map<?, ?> map) {
            Map<String, Object> result = new LinkedHashMap<>();
            for (Map.Entry<?, ?> entry : map.entrySet()) {
                result.put(String.valueOf(entry.getKey()), entry.getValue());
            }
            return result;
        }
        throw new IllegalArgumentException("JSON body must be an object");
    }

    private static void writeValue(StringBuilder builder, Object value) {
        if (value == null) {
            builder.append("null");
            return;
        }
        if (value instanceof String string) {
            builder.append('"').append(escape(string)).append('"');
            return;
        }
        if (value instanceof Number || value instanceof Boolean) {
            builder.append(value);
            return;
        }
        if (value instanceof Instant instant) {
            builder.append('"').append(instant.toString()).append('"');
            return;
        }
        if (value instanceof Enum<?> enumValue) {
            builder.append('"').append(enumValue.name()).append('"');
            return;
        }
        if (value instanceof Map<?, ?> map) {
            builder.append('{');
            boolean first = true;
            for (Map.Entry<?, ?> entry : map.entrySet()) {
                if (!first) {
                    builder.append(',');
                }
                first = false;
                builder.append('"').append(escape(String.valueOf(entry.getKey()))).append('"').append(':');
                writeValue(builder, entry.getValue());
            }
            builder.append('}');
            return;
        }
        if (value instanceof Iterable<?> iterable) {
            builder.append('[');
            boolean first = true;
            for (Object element : iterable) {
                if (!first) {
                    builder.append(',');
                }
                first = false;
                writeValue(builder, element);
            }
            builder.append(']');
            return;
        }
        if (value.getClass().isArray()) {
            builder.append('[');
            int length = Array.getLength(value);
            for (int i = 0; i < length; i++) {
                if (i > 0) {
                    builder.append(',');
                }
                writeValue(builder, Array.get(value, i));
            }
            builder.append(']');
            return;
        }
        builder.append('{');
        Field[] fields = value.getClass().getFields();
        boolean first = true;
        for (Field field : fields) {
            if (Modifier.isStatic(field.getModifiers())) {
                continue;
            }
            if (!first) {
                builder.append(',');
            }
            first = false;
            try {
                builder.append('"').append(escape(field.getName())).append('"').append(':');
                writeValue(builder, field.get(value));
            } catch (IllegalAccessException exception) {
                throw new IllegalStateException(exception);
            }
        }
        builder.append('}');
    }

    private static String escape(String value) {
        return value
                .replace("\\", "\\\\")
                .replace("\"", "\\\"")
                .replace("\b", "\\b")
                .replace("\f", "\\f")
                .replace("\n", "\\n")
                .replace("\r", "\\r")
                .replace("\t", "\\t");
    }

    private static final class Parser {
        private final String json;
        private int index;

        private Parser(String json) {
            this.json = json == null ? "" : json.trim();
        }

        private Object parseValue() {
            skipWhitespace();
            if (index >= json.length()) {
                return null;
            }
            char current = json.charAt(index);
            return switch (current) {
                case '{' -> parseObject();
                case '[' -> parseArray();
                case '"' -> parseString();
                case 't', 'f' -> parseBoolean();
                case 'n' -> parseNull();
                default -> parseNumber();
            };
        }

        private Map<String, Object> parseObject() {
            index++;
            Map<String, Object> result = new LinkedHashMap<>();
            skipWhitespace();
            while (index < json.length() && json.charAt(index) != '}') {
                String key = parseString();
                skipWhitespace();
                expect(':');
                Object value = parseValue();
                result.put(key, value);
                skipWhitespace();
                if (peek(',') ) {
                    index++;
                    skipWhitespace();
                } else {
                    break;
                }
            }
            expect('}');
            return result;
        }

        private List<Object> parseArray() {
            index++;
            List<Object> result = new ArrayList<>();
            skipWhitespace();
            while (index < json.length() && json.charAt(index) != ']') {
                result.add(parseValue());
                skipWhitespace();
                if (peek(',')) {
                    index++;
                    skipWhitespace();
                } else {
                    break;
                }
            }
            expect(']');
            return result;
        }

        private String parseString() {
            expect('"');
            StringBuilder builder = new StringBuilder();
            while (index < json.length()) {
                char current = json.charAt(index++);
                if (current == '"') {
                    break;
                }
                if (current == '\\') {
                    if (index >= json.length()) {
                        break;
                    }
                    char escaped = json.charAt(index++);
                    builder.append(switch (escaped) {
                        case '"' -> '"';
                        case '\\' -> '\\';
                        case '/' -> '/';
                        case 'b' -> '\b';
                        case 'f' -> '\f';
                        case 'n' -> '\n';
                        case 'r' -> '\r';
                        case 't' -> '\t';
                        default -> escaped;
                    });
                } else {
                    builder.append(current);
                }
            }
            return builder.toString();
        }

        private Object parseBoolean() {
            if (json.startsWith("true", index)) {
                index += 4;
                return Boolean.TRUE;
            }
            if (json.startsWith("false", index)) {
                index += 5;
                return Boolean.FALSE;
            }
            throw new IllegalArgumentException("Invalid boolean value");
        }

        private Object parseNull() {
            if (!json.startsWith("null", index)) {
                throw new IllegalArgumentException("Invalid null value");
            }
            index += 4;
            return null;
        }

        private Number parseNumber() {
            int start = index;
            while (index < json.length()) {
                char current = json.charAt(index);
                if ((current >= '0' && current <= '9') || current == '-' || current == '+' || current == '.' || current == 'e' || current == 'E') {
                    index++;
                } else {
                    break;
                }
            }
            String token = json.substring(start, index);
            if (token.contains(".") || token.contains("e") || token.contains("E")) {
                return Double.parseDouble(token);
            }
            return Long.parseLong(token);
        }

        private void skipWhitespace() {
            while (index < json.length() && Character.isWhitespace(json.charAt(index))) {
                index++;
            }
        }

        private boolean peek(char expected) {
            return index < json.length() && json.charAt(index) == expected;
        }

        private void expect(char expected) {
            skipWhitespace();
            if (index >= json.length() || json.charAt(index) != expected) {
                throw new IllegalArgumentException("Expected '" + expected + "' at position " + index);
            }
            index++;
        }
    }
}