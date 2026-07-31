package com.homeaffairs;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetSocketAddress;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;

public class ApiServer {

    private final BackendState state;
    private HttpServer server;

    public ApiServer(BackendState state) {
        this.state = state;
    }

    public void start(int port) throws IOException {
        server = HttpServer.create(new InetSocketAddress(port), 0);
        server.createContext("/api", new ApiHandler());
        server.setExecutor(null);
        server.start();
    }

    private final class ApiHandler implements HttpHandler {

        @Override
        public void handle(HttpExchange exchange) throws IOException {
            applyCors(exchange);
            if ("OPTIONS".equalsIgnoreCase(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(204, -1);
                exchange.close();
                return;
            }
            try {
                String path = exchange.getRequestURI().getPath().substring("/api".length());
                List<String> segments = pathSegments(path);
                String method = exchange.getRequestMethod().toUpperCase();

                if (segments.isEmpty() || segments.size() == 1 && "health".equals(segments.get(0)) && "GET".equals(method)) {
                    sendJson(exchange, 200, state.health());
                    return;
                }

                if (segments.size() >= 2 && "auth".equals(segments.get(0))) {
                    handleAuth(exchange, method, segments);
                    return;
                }
                if (segments.size() >= 1 && "users".equals(segments.get(0))) {
                    handleUsers(exchange, method, segments);
                    return;
                }
                if (segments.size() >= 1 && "folders".equals(segments.get(0))) {
                    handleFolders(exchange, method, segments);
                    return;
                }
                if (segments.size() >= 1 && "documents".equals(segments.get(0))) {
                    handleDocuments(exchange, method, segments);
                    return;
                }
                if (segments.size() >= 1 && "workflows".equals(segments.get(0))) {
                    handleWorkflows(exchange, method, segments);
                    return;
                }
                if (segments.size() >= 1 && "notifications".equals(segments.get(0))) {
                    handleNotifications(exchange, method, segments);
                    return;
                }
                if (segments.size() >= 2 && "citizen".equals(segments.get(0)) && "applications".equals(segments.get(1))) {
                    handleCitizenApplications(exchange, method, segments);
                    return;
                }
                if (segments.size() >= 1 && "dashboard".equals(segments.get(0))) {
                    handleDashboard(exchange, method);
                    return;
                }
                if (segments.size() >= 2 && "design".equals(segments.get(0)) && "traceability".equals(segments.get(1)) && "GET".equals(method)) {
                    sendJson(exchange, 200, state.getTraceability());
                    return;
                }

                sendJson(exchange, 404, error("Route not found"));
            } catch (HttpException exception) {
                sendJson(exchange, exception.getStatusCode(), error(exception.getMessage()));
            } catch (Exception exception) {
                sendJson(exchange, 500, error(exception.getMessage() == null ? "Unexpected server error" : exception.getMessage()));
            }
        }

        private void handleAuth(HttpExchange exchange, String method, List<String> segments) throws IOException {
            if (segments.size() == 2 && "login".equals(segments.get(1)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.login(readJsonBody(exchange)));
                return;
            }
            if (segments.size() == 2 && "me".equals(segments.get(1)) && "GET".equals(method)) {
                sendJson(exchange, 200, state.me(requireToken(exchange)));
                return;
            }
            if (segments.size() == 3 && "password-reset".equals(segments.get(1)) && "start".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.startPasswordReset(readJsonBody(exchange)));
                return;
            }
            if (segments.size() == 3 && "password-reset".equals(segments.get(1)) && "complete".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.completePasswordReset(readJsonBody(exchange)));
                return;
            }
            sendJson(exchange, 404, error("Route not found"));
        }

        private void handleUsers(HttpExchange exchange, String method, List<String> segments) throws IOException {
            if (segments.size() == 1 && "GET".equals(method)) {
                sendJson(exchange, 200, state.listUsers(requireToken(exchange)));
                return;
            }
            if (segments.size() == 1 && "POST".equals(method)) {
                sendJson(exchange, 200, state.createUser(requireToken(exchange), readJsonBody(exchange)));
                return;
            }
            if (segments.size() == 2 && "PATCH".equals(method)) {
                sendJson(exchange, 200, state.updateUser(requireToken(exchange), Long.parseLong(segments.get(1)), readJsonBody(exchange)));
                return;
            }
            sendJson(exchange, 404, error("Route not found"));
        }

        private void handleFolders(HttpExchange exchange, String method, List<String> segments) throws IOException {
            if (segments.size() == 1 && "GET".equals(method)) {
                sendJson(exchange, 200, state.listFolders(requireToken(exchange)));
                return;
            }
            if (segments.size() == 1 && "POST".equals(method)) {
                sendJson(exchange, 200, state.createFolder(requireToken(exchange), readJsonBody(exchange)));
                return;
            }
            sendJson(exchange, 404, error("Route not found"));
        }

        private void handleDocuments(HttpExchange exchange, String method, List<String> segments) throws IOException {
            if (segments.size() == 1 && "GET".equals(method)) {
                sendJson(exchange, 200, state.listDocuments(requireToken(exchange), queryParams(exchange.getRequestURI())));
                return;
            }
            if (segments.size() == 1 && "POST".equals(method)) {
                sendJson(exchange, 200, state.uploadDocument(requireToken(exchange), readJsonBody(exchange)));
                return;
            }
            if (segments.size() == 2 && "GET".equals(method)) {
                sendJson(exchange, 200, state.getDocument(requireToken(exchange), Long.parseLong(segments.get(1))));
                return;
            }
            if (segments.size() == 3 && "download".equals(segments.get(2)) && "GET".equals(method)) {
                byte[] bytes = state.rawDownload(requireToken(exchange), Long.parseLong(segments.get(1)));
                exchange.getResponseHeaders().set("Content-Type", state.mimeType(Long.parseLong(segments.get(1))));
                exchange.getResponseHeaders().set("Content-Disposition", "attachment; filename=\"" + state.fileName(Long.parseLong(segments.get(1))) + "\"");
                exchange.sendResponseHeaders(200, bytes.length);
                try (OutputStream output = exchange.getResponseBody()) {
                    output.write(bytes);
                }
                return;
            }
            if (segments.size() == 3 && "versions".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.addVersion(requireToken(exchange), Long.parseLong(segments.get(1)), readJsonBody(exchange)));
                return;
            }
            if (segments.size() == 3 && "comments".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.addComment(requireToken(exchange), Long.parseLong(segments.get(1)), readJsonBody(exchange)));
                return;
            }
            if (segments.size() == 3 && "share".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.shareDocument(requireToken(exchange), Long.parseLong(segments.get(1)), readJsonBody(exchange)));
                return;
            }
            if (segments.size() == 3 && "trash".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.trashDocument(requireToken(exchange), Long.parseLong(segments.get(1))));
                return;
            }
            if (segments.size() == 3 && "restore".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.restoreDocument(requireToken(exchange), Long.parseLong(segments.get(1))));
                return;
            }
            if (segments.size() == 3 && "submit".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.submitForApproval(requireToken(exchange), Long.parseLong(segments.get(1)), readJsonBody(exchange)));
                return;
            }
            sendJson(exchange, 404, error("Route not found"));
        }

        private void handleWorkflows(HttpExchange exchange, String method, List<String> segments) throws IOException {
            if (segments.size() == 1 && "GET".equals(method)) {
                sendJson(exchange, 200, state.listPendingWorkflows(requireToken(exchange)));
                return;
            }
            if (segments.size() == 3 && "approve".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.reviewWorkflow(requireToken(exchange), Long.parseLong(segments.get(1)), true, readJsonBody(exchange)));
                return;
            }
            if (segments.size() == 3 && "reject".equals(segments.get(2)) && "POST".equals(method)) {
                sendJson(exchange, 200, state.reviewWorkflow(requireToken(exchange), Long.parseLong(segments.get(1)), false, readJsonBody(exchange)));
                return;
            }
            sendJson(exchange, 404, error("Route not found"));
        }

        private void handleNotifications(HttpExchange exchange, String method, List<String> segments) throws IOException {
            if (segments.size() == 1 && "GET".equals(method)) {
                sendJson(exchange, 200, state.listNotifications(requireToken(exchange)));
                return;
            }
            if (segments.size() == 2 && "POST".equals(method)) {
                sendJson(exchange, 200, state.markNotificationRead(requireToken(exchange), Long.parseLong(segments.get(1))));
                return;
            }
            sendJson(exchange, 404, error("Route not found"));
        }

        private void handleCitizenApplications(HttpExchange exchange, String method, List<String> segments) throws IOException {
            if (segments.size() == 2 && "GET".equals(method)) {
                sendJson(exchange, 200, state.listCitizenApplications(requireToken(exchange)));
                return;
            }
            if (segments.size() == 2 && "POST".equals(method)) {
                sendJson(exchange, 200, state.submitCitizenApplication(requireToken(exchange), readJsonBody(exchange)));
                return;
            }
            sendJson(exchange, 404, error("Route not found"));
        }

        private void handleDashboard(HttpExchange exchange, String method) throws IOException {
            if ("GET".equals(method)) {
                sendJson(exchange, 200, state.dashboard(requireToken(exchange)));
                return;
            }
            sendJson(exchange, 404, error("Route not found"));
        }

        private String requireToken(HttpExchange exchange) {
            String bearer = exchange.getRequestHeaders().getFirst("Authorization");
            if (bearer != null && bearer.startsWith("Bearer ")) {
                return bearer.substring("Bearer ".length()).trim();
            }
            String token = exchange.getRequestHeaders().getFirst("X-Auth-Token");
            if (token != null && !token.isBlank()) {
                return token.trim();
            }
            throw new HttpException(401, "Authentication token missing");
        }

        private Map<String, Object> readJsonBody(HttpExchange exchange) throws IOException {
            try (InputStream input = exchange.getRequestBody()) {
                byte[] bytes = input.readAllBytes();
                if (bytes.length == 0) {
                    return new LinkedHashMap<>();
                }
                return Json.parseObject(new String(bytes, StandardCharsets.UTF_8));
            }
        }

        private List<String> pathSegments(String path) {
            if (path == null || path.isBlank() || "/".equals(path)) {
                return List.of();
            }
            String trimmed = path.startsWith("/") ? path.substring(1) : path;
            return List.of(trimmed.split("/"));
        }

        private Map<String, String> queryParams(URI uri) {
            Map<String, String> params = new LinkedHashMap<>();
            String query = uri.getQuery();
            if (query == null || query.isBlank()) {
                return params;
            }
            for (String pair : query.split("&")) {
                String[] parts = pair.split("=", 2);
                String key = decode(parts[0]);
                String value = parts.length > 1 ? decode(parts[1]) : "";
                params.put(key, value);
            }
            return params;
        }

        private String decode(String value) {
            return java.net.URLDecoder.decode(value, StandardCharsets.UTF_8);
        }

        private void sendJson(HttpExchange exchange, int statusCode, Object body) throws IOException {
            byte[] payload = Json.stringify(body).getBytes(StandardCharsets.UTF_8);
            exchange.getResponseHeaders().set("Content-Type", "application/json; charset=utf-8");
            exchange.sendResponseHeaders(statusCode, payload.length);
            try (OutputStream output = exchange.getResponseBody()) {
                output.write(payload);
            }
        }

        private Map<String, Object> error(String message) {
            return Map.of("error", message);
        }

        private void applyCors(HttpExchange exchange) {
            exchange.getResponseHeaders().add("Access-Control-Allow-Origin", "*");
            exchange.getResponseHeaders().add("Access-Control-Allow-Methods", "GET,POST,PATCH,DELETE,OPTIONS");
            exchange.getResponseHeaders().add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Auth-Token");
        }
    }
}