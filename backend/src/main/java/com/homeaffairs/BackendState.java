package com.homeaffairs;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.Duration;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Base64;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

import com.homeaffairs.Domain.ApplicationStatus;
import com.homeaffairs.Domain.AuditLog;
import com.homeaffairs.Domain.CitizenApplication;
import com.homeaffairs.Domain.Comment;
import com.homeaffairs.Domain.Document;
import com.homeaffairs.Domain.DocumentStatus;
import com.homeaffairs.Domain.DocumentVersion;
import com.homeaffairs.Domain.Folder;
import com.homeaffairs.Domain.Notification;
import com.homeaffairs.Domain.NotificationType;
import com.homeaffairs.Domain.Permission;
import com.homeaffairs.Domain.ResetToken;
import com.homeaffairs.Domain.Role;
import com.homeaffairs.Domain.User;
import com.homeaffairs.Domain.Workflow;
import com.homeaffairs.Domain.WorkflowStatus;

public class BackendState {

    private static final DateTimeFormatter DATE = DateTimeFormatter.ISO_LOCAL_DATE;

    private final AtomicLong ids = new AtomicLong(1);
    private final Map<Long, User> users = new LinkedHashMap<>();
    private final Map<Long, Folder> folders = new LinkedHashMap<>();
    private final Map<Long, Document> documents = new LinkedHashMap<>();
    private final Map<Long, DocumentVersion> versions = new LinkedHashMap<>();
    private final Map<Long, Comment> comments = new LinkedHashMap<>();
    private final Map<Long, Permission> permissions = new LinkedHashMap<>();
    private final Map<Long, Workflow> workflows = new LinkedHashMap<>();
    private final Map<Long, Notification> notifications = new LinkedHashMap<>();
    private final Map<Long, CitizenApplication> applications = new LinkedHashMap<>();
    private final Map<String, ResetToken> resetTokens = new ConcurrentHashMap<>();
    private final Map<String, Session> sessions = new ConcurrentHashMap<>();
    private final Map<Long, AuditLog> auditLogs = new LinkedHashMap<>();
    private final Path storageRoot = Paths.get("backend-storage");

    private final int sessionTimeoutMinutes = 3;
    private final int lockoutThreshold = 5;
    private final int lockoutMinutes = 15;
    private final String defaultPassword = "ChangeMe123!";

    private long generalFolderId;
    private long citizenFolderId;

    public BackendState() {
        try {
            Files.createDirectories(storageRoot);
        } catch (IOException exception) {
            throw new IllegalStateException("Unable to create storage directory", exception);
        }
    }

    public synchronized void seed() {
        if (!users.isEmpty()) {
            return;
        }
        User admin = createUser("System Administrator", "admin@homeaffairs.gov.za", Role.IT_SUPERUSER, defaultPassword);
        User reviewer = createUser("Workflow Reviewer", "reviewer@homeaffairs.gov.za", Role.REVIEWER, defaultPassword);
        User employee = createUser("Document Officer", "employee@homeaffairs.gov.za", Role.EMPLOYEE, defaultPassword);
        User citizen = createUser("Citizen Demo", "citizen@example.com", Role.CITIZEN, defaultPassword);

        Folder general = createFolder("General Policies", "Shared documents for all employees", true, null);
        Folder citizenFolder = createFolder("Citizen Applications", "Submissions received from the portal", false, null);
        generalFolderId = general.id;
        citizenFolderId = citizenFolder.id;

        uploadDocumentInternal(admin, general.id, "Home Affairs Leave Policy", "General access policy document", "leave-policy.pdf", "application/pdf", "Seed policy text".getBytes(StandardCharsets.UTF_8), false);
        Document seeded = uploadDocumentInternal(employee, general.id, "ID Application Checklist", "Internal checklist for ID requests", "id-checklist.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Checklist seed".getBytes(StandardCharsets.UTF_8), false);
        addCommentInternal(seeded.id, reviewer, "Please confirm the latest signature sequence.");

        submitForApprovalInternal(employee, seeded.id, "Initial review requested");
        addNotification(admin.id, NotificationType.USER_ADMIN, "Backend ready", "Seed users and documents were created for the demo.", "System", null);
        createAudit(admin.id, "SEED", "system", null, "Demo data created");
        findUserByEmail(citizen.email).lastLoginAt = Instant.now();
    }

    public synchronized Map<String, Object> health() {
        return response("status", "ok", "service", "home-affairs-backend", "users", users.size(), "documents", documents.size());
    }

    public synchronized Map<String, Object> login(Map<String, Object> body) {
        String email = requiredString(body, "email");
        String password = requiredString(body, "password");
        User user = findUserByEmail(email);
        if (user == null) {
            throw new HttpException(401, "Invalid credentials");
        }
        if (!user.active) {
            throw new HttpException(403, "Account is inactive");
        }
        if (user.lockedUntil != null && user.lockedUntil.isAfter(Instant.now())) {
            throw new HttpException(423, "Account is locked until " + user.lockedUntil);
        }
        if (!user.passwordHash.equals(hashPassword(password))) {
            user.failedAttempts++;
            if (user.failedAttempts >= lockoutThreshold) {
                user.lockedUntil = Instant.now().plus(Duration.ofMinutes(lockoutMinutes));
                user.failedAttempts = 0;
                addNotification(user.id, NotificationType.USER_ADMIN, "Account locked", "Too many invalid sign-in attempts.", "User", user.id);
            }
            createAudit(user.id, "LOGIN_FAILED", "user", user.id, "Invalid password");
            throw new HttpException(401, "Invalid credentials");
        }
        user.failedAttempts = 0;
        user.lockedUntil = null;
        user.lastLoginAt = Instant.now();
        String token = UUID.randomUUID().toString();
        Instant expiresAt = Instant.now().plus(Duration.ofMinutes(sessionTimeoutMinutes));
        sessions.put(token, new Session(user.id, expiresAt));
        createAudit(user.id, "LOGIN_SUCCESS", "user", user.id, "User authenticated");
        return response(
                "token", token,
                "expiresAt", expiresAt.toString(),
                "sessionTimeoutMinutes", sessionTimeoutMinutes,
                "user", userView(user)
        );
    }

    public synchronized Map<String, Object> me(String token) {
        return response("user", userView(requireUser(token)));
    }

    public synchronized Map<String, Object> startPasswordReset(Map<String, Object> body) {
        String email = requiredString(body, "email");
        User user = findUserByEmail(email);
        if (user == null) {
            throw new HttpException(404, "No account found for that email address");
        }
        ResetToken resetToken = new ResetToken();
        resetToken.token = UUID.randomUUID().toString();
        resetToken.userId = user.id;
        resetToken.expiresAt = Instant.now().plus(Duration.ofMinutes(30));
        resetTokens.put(resetToken.token, resetToken);
        addNotification(user.id, NotificationType.PASSWORD_RESET, "Password reset requested", "Use the provided token to reset your password.", "User", user.id);
        return response("email", email, "resetToken", resetToken.token, "expiresAt", resetToken.expiresAt.toString());
    }

    public synchronized Map<String, Object> completePasswordReset(Map<String, Object> body) {
        String token = requiredString(body, "token");
        String newPassword = requiredString(body, "newPassword");
        ResetToken resetToken = resetTokens.get(token);
        if (resetToken == null || resetToken.usedAt != null || resetToken.expiresAt.isBefore(Instant.now())) {
            throw new HttpException(400, "Reset token is invalid or expired");
        }
        User user = users.get(resetToken.userId);
        if (user == null) {
            throw new HttpException(404, "User not found");
        }
        user.passwordHash = hashPassword(newPassword);
        user.failedAttempts = 0;
        user.lockedUntil = null;
        resetToken.usedAt = Instant.now();
        addNotification(user.id, NotificationType.PASSWORD_RESET, "Password updated", "Your password has been reset successfully.", "User", user.id);
        return response("success", true);
    }

    public synchronized Map<String, Object> listUsers(String token) {
        requireRole(token, Role.IT_SUPERUSER);
        List<Map<String, Object>> result = users.values().stream().map(this::userView).toList();
        return response("items", result);
    }

    public synchronized Map<String, Object> createUser(String token, Map<String, Object> body) {
        requireRole(token, Role.IT_SUPERUSER);
        String fullName = requiredString(body, "fullName");
        String email = requiredString(body, "email");
        Role role = Role.valueOf(requiredString(body, "role"));
        String password = stringValue(body, "password", defaultPassword);
        if (findUserByEmail(email) != null) {
            throw new HttpException(409, "A user with that email already exists");
        }
        User user = createUser(fullName, email, role, password);
        addNotification(user.id, NotificationType.USER_ADMIN, "Welcome", "Your account has been created.", "User", user.id);
        return response("user", userView(user));
    }

    public synchronized Map<String, Object> updateUser(String token, long userId, Map<String, Object> body) {
        requireRole(token, Role.IT_SUPERUSER);
        User user = requireUserById(userId);
        if (body.containsKey("active")) {
            user.active = booleanValue(body, "active", user.active);
        }
        if (body.containsKey("role")) {
            user.role = Role.valueOf(requiredString(body, "role"));
        }
        if (body.containsKey("unlock") && booleanValue(body, "unlock", false)) {
            user.lockedUntil = null;
            user.failedAttempts = 0;
        }
        if (body.containsKey("lock") && booleanValue(body, "lock", false)) {
            user.lockedUntil = Instant.now().plus(Duration.ofMinutes(lockoutMinutes));
        }
        createAudit(currentUser(token).id, "USER_UPDATE", "user", user.id, "User account updated");
        return response("user", userView(user));
    }

    public synchronized Map<String, Object> listFolders(String token) {
        requireUser(token);
        List<Map<String, Object>> result = folders.values().stream().map(this::folderView).toList();
        return response("items", result);
    }

    public synchronized Map<String, Object> createFolder(String token, Map<String, Object> body) {
        User user = requireUser(token);
        String name = requiredString(body, "name");
        String description = stringValue(body, "description", "");
        boolean generalAccess = booleanValue(body, "generalAccess", false);
        Long parentFolderId = optionalLong(body, "parentFolderId");
        Folder folder = createFolder(name, description, generalAccess, parentFolderId);
        createAudit(user.id, "CREATE_FOLDER", "folder", folder.id, folder.name);
        return response("folder", folderView(folder));
    }

    public synchronized Map<String, Object> listDocuments(String token, Map<String, String> query) {
        User user = requireUser(token);
        String search = normalize(query.get("query"));
        String fileType = normalize(query.get("fileType"));
        Long folderId = parseOptionalLong(query.get("folderId"));
        boolean deleted = Boolean.parseBoolean(stringOrDefault(query.get("deleted"), "false"));
        String from = query.get("from");
        String to = query.get("to");
        Instant fromInstant = from == null || from.isBlank() ? null : LocalDate.parse(from).atStartOfDay(ZoneId.systemDefault()).toInstant();
        Instant toInstant = to == null || to.isBlank() ? null : LocalDate.parse(to).plusDays(1).atStartOfDay(ZoneId.systemDefault()).toInstant();

        List<Map<String, Object>> result = documents.values().stream()
                .filter(document -> document.deleted == deleted)
                .filter(document -> folderId == null || document.folderId == folderId)
                .filter(document -> search == null || matches(document, search))
                .filter(document -> fileType == null || (document.fileType != null && document.fileType.toLowerCase(Locale.ROOT).contains(fileType)))
                .filter(document -> fromInstant == null || !document.updatedAt.isBefore(fromInstant))
                .filter(document -> toInstant == null || document.updatedAt.isBefore(toInstant))
                .filter(document -> canViewDocument(user, document))
                .sorted(Comparator.comparing((Document document) -> document.updatedAt).reversed())
                .map(document -> documentSummary(document, user))
                .toList();
        return response("items", result);
    }

    public synchronized Map<String, Object> uploadDocument(String token, Map<String, Object> body) {
        User user = requireUser(token);
        String title = requiredString(body, "title");
        String description = stringValue(body, "description", "");
        String fileName = requiredString(body, "fileName");
        String mimeType = stringValue(body, "mimeType", "application/octet-stream");
        long folderId = optionalLong(body, "folderId") != null ? optionalLong(body, "folderId") : generalFolderId;
        byte[] bytes = Base64.getDecoder().decode(requiredString(body, "contentBase64"));

        Document existingDuplicate = documents.values().stream()
                .filter(document -> !document.deleted)
                .filter(document -> Objects.equals(document.checksum, checksum(bytes)))
                .findFirst()
                .orElse(null);
        if (existingDuplicate != null) {
            throw new HttpException(409, "Duplicate upload detected for document " + existingDuplicate.id);
        }
        Document document = uploadDocumentInternal(user, folderId, title, description, fileName, mimeType, bytes, true);
        createAudit(user.id, "UPLOAD_DOCUMENT", "document", document.id, document.title);
        return response("document", documentDetail(document, user));
    }

    public synchronized Map<String, Object> getDocument(String token, long documentId) {
        User user = requireUser(token);
        Document document = requireDocument(documentId);
        ensureDocumentAccess(user, document);
        return response("document", documentDetail(document, user));
    }

    public synchronized Map<String, Object> addVersion(String token, long documentId, Map<String, Object> body) {
        User user = requireUser(token);
        Document document = requireDocument(documentId);
        ensureDocumentEditAccess(user, document);
        String fileName = requiredString(body, "fileName");
        String mimeType = stringValue(body, "mimeType", "application/octet-stream");
        byte[] bytes = Base64.getDecoder().decode(requiredString(body, "contentBase64"));
        DocumentVersion version = addVersionInternal(document, user, fileName, mimeType, bytes);
        createAudit(user.id, "ADD_VERSION", "document", document.id, "New version " + version.versionNumber);
        return response("version", versionView(version));
    }

    public synchronized Map<String, Object> addComment(String token, long documentId, Map<String, Object> body) {
        User user = requireUser(token);
        Document document = requireDocument(documentId);
        ensureDocumentCommentAccess(user, document);
        String commentBody = requiredString(body, "body");
        Comment comment = addCommentInternal(document.id, user, commentBody);
        notifyCollaborators(document, NotificationType.DOCUMENT_UPDATED, "New comment", user.fullName + " commented on " + document.title, user.id);
        return response("comment", commentView(comment));
    }

    public synchronized Map<String, Object> shareDocument(String token, long documentId, Map<String, Object> body) {
        User user = requireUser(token);
        Document document = requireDocument(documentId);
        ensureDocumentEditAccess(user, document);
        String email = requiredString(body, "email");
        User target = findUserByEmail(email);
        if (target == null) {
            throw new HttpException(404, "Target user not found");
        }
        Permission permission = findPermission(document.id, target.id);
        boolean created = false;
        if (permission == null) {
            permission = new Permission();
            permission.id = ids.getAndIncrement();
            permission.documentId = document.id;
            permission.userId = target.id;
            permissions.put(permission.id, permission);
            document.permissionIds.add(permission.id);
            created = true;
        }
        permission.canView = booleanValue(body, "canView", true);
        permission.canEdit = booleanValue(body, "canEdit", false);
        permission.canComment = booleanValue(body, "canComment", true);
        permission.canShare = booleanValue(body, "canShare", false);
        permission.canApprove = booleanValue(body, "canApprove", false);
        permission.grantedByUserId = user.id;
        permission.grantedAt = Instant.now();
        addNotification(target.id, NotificationType.DOCUMENT_SHARED, "Document shared", document.title + " was shared with you.", "Document", document.id);
        createAudit(user.id, "SHARE_DOCUMENT", "document", document.id, (created ? "Created" : "Updated") + " permission for " + email);
        return response("permission", permissionView(permission));
    }

    public synchronized Map<String, Object> trashDocument(String token, long documentId) {
        User user = requireUser(token);
        Document document = requireDocument(documentId);
        ensureDocumentEditAccess(user, document);
        document.deleted = true;
        document.deletedAt = Instant.now();
        document.status = DocumentStatus.TRASHED;
        document.updatedAt = Instant.now();
        notifyCollaborators(document, NotificationType.DOCUMENT_UPDATED, "Document moved to recycle bin", document.title + " was moved to the recycle bin.", user.id);
        createAudit(user.id, "TRASH_DOCUMENT", "document", document.id, document.title);
        return response("document", documentSummary(document, user));
    }

    public synchronized Map<String, Object> restoreDocument(String token, long documentId) {
        User user = requireUser(token);
        Document document = requireDocument(documentId);
        ensureDocumentEditAccess(user, document);
        document.deleted = false;
        document.deletedAt = null;
        document.status = DocumentStatus.DRAFT;
        document.updatedAt = Instant.now();
        createAudit(user.id, "RESTORE_DOCUMENT", "document", document.id, document.title);
        return response("document", documentSummary(document, user));
    }

    public synchronized Map<String, Object> submitForApproval(String token, long documentId, Map<String, Object> body) {
        User user = requireUser(token);
        Document document = requireDocument(documentId);
        ensureDocumentEditAccess(user, document);
        String notes = stringValue(body, "notes", "");
        Workflow workflow = submitForApprovalInternal(user, document.id, notes);
        return response("workflow", workflowView(workflow));
    }

    public synchronized Map<String, Object> listPendingWorkflows(String token) {
        User user = requireUser(token);
        List<Map<String, Object>> result = workflows.values().stream()
                .filter(workflow -> workflow.status == WorkflowStatus.PENDING)
                .filter(workflow -> user.role == Role.IT_SUPERUSER || user.role == Role.REVIEWER || workflow.requestedByUserId == user.id)
                .sorted(Comparator.comparing((Workflow workflow) -> workflow.updatedAt).reversed())
                .map(this::workflowView)
                .toList();
        return response("items", result);
    }

    public synchronized Map<String, Object> reviewWorkflow(String token, long workflowId, boolean approve, Map<String, Object> body) {
        User user = requireUser(token);
        if (!(user.role == Role.IT_SUPERUSER || user.role == Role.REVIEWER)) {
            throw new HttpException(403, "Only reviewers can decide workflows");
        }
        Workflow workflow = requireWorkflow(workflowId);
        Document document = requireDocument(workflow.documentId);
        String notes = stringValue(body, "notes", "");
        workflow.status = approve ? WorkflowStatus.APPROVED : WorkflowStatus.REJECTED;
        workflow.notes = notes;
        workflow.reviewerUserId = user.id;
        workflow.updatedAt = Instant.now();
        document.status = approve ? DocumentStatus.APPROVED : DocumentStatus.REJECTED;
        document.updatedAt = Instant.now();
        addNotification(document.ownerUserId, approve ? NotificationType.APPROVED : NotificationType.REJECTED, approve ? "Document approved" : "Document rejected", document.title + " was " + (approve ? "approved" : "rejected") + ".", "Workflow", workflow.id);
        notifyCollaborators(document, approve ? NotificationType.APPROVED : NotificationType.REJECTED, approve ? "Workflow approved" : "Workflow rejected", document.title + " was " + (approve ? "approved" : "rejected") + ".", user.id);
        createAudit(user.id, approve ? "APPROVE_WORKFLOW" : "REJECT_WORKFLOW", "workflow", workflow.id, notes);
        return response("workflow", workflowView(workflow));
    }

    public synchronized Map<String, Object> listNotifications(String token) {
        User user = requireUser(token);
        List<Map<String, Object>> result = notifications.values().stream()
                .filter(notification -> notification.userId == user.id)
                .sorted(Comparator.comparing((Notification notification) -> notification.createdAt).reversed())
                .map(this::notificationView)
                .toList();
        return response("items", result);
    }

    public synchronized Map<String, Object> markNotificationRead(String token, long notificationId) {
        User user = requireUser(token);
        Notification notification = notifications.get(notificationId);
        if (notification == null || notification.userId != user.id) {
            throw new HttpException(404, "Notification not found");
        }
        notification.read = true;
        return response("notification", notificationView(notification));
    }

    public synchronized Map<String, Object> listCitizenApplications(String token) {
        User user = requireUser(token);
        List<Map<String, Object>> result = applications.values().stream()
                .filter(application -> user.role != Role.CITIZEN || application.submittedByUserId == user.id)
                .sorted(Comparator.comparingLong((CitizenApplication application) -> application.submittedAt).reversed())
                .map(this::applicationView)
                .toList();
        return response("items", result);
    }

    public synchronized Map<String, Object> submitCitizenApplication(String token, Map<String, Object> body) {
        User user = requireUser(token);
        String applicationType = requiredString(body, "applicationType");
        String fullName = requiredString(body, "fullName");
        String email = requiredString(body, "email");
        String idNumber = requiredString(body, "idNumber");
        String phoneNumber = requiredString(body, "phoneNumber");
        String notes = stringValue(body, "notes", "");

        CitizenApplication application = new CitizenApplication();
        application.id = ids.getAndIncrement();
        application.applicationNumber = "HA-APP-" + String.format(Locale.ROOT, "%05d", application.id);
        application.applicationType = applicationType;
        application.fullName = fullName;
        application.email = email;
        application.idNumber = idNumber;
        application.phoneNumber = phoneNumber;
        application.notes = notes;
        application.submittedByUserId = user.id;

        if (body.containsKey("attachment")) {
            Map<String, Object> attachment = asMap(body.get("attachment"));
            String fileName = requiredString(attachment, "fileName");
            String mimeType = stringValue(attachment, "mimeType", "application/octet-stream");
            byte[] bytes = Base64.getDecoder().decode(requiredString(attachment, "contentBase64"));
            Document document = uploadDocumentInternal(user, citizenFolderId, applicationType + " Supporting Document", notes, fileName, mimeType, bytes, true);
            application.attachedDocumentId = document.id;
        }

        applications.put(application.id, application);
        addNotification(resolveReviewer().id, NotificationType.APPLICATION_SUBMITTED, "Citizen application received", application.fullName + " submitted a new application.", "CitizenApplication", application.id);
        createAudit(user.id, "SUBMIT_APPLICATION", "application", application.id, application.applicationNumber);
        return response("application", applicationView(application));
    }

    public synchronized Map<String, Object> dashboard(String token) {
        User user = requireUser(token);
        List<Map<String, Object>> recentDocuments = documents.values().stream()
                .filter(document -> canViewDocument(user, document))
                .sorted(Comparator.comparing((Document document) -> document.updatedAt).reversed())
                .limit(5)
                .map(document -> documentSummary(document, user))
                .toList();
        Map<String, Object> data = new LinkedHashMap<>();
        data.put("totalDocuments", documents.values().stream().filter(document -> !document.deleted).count());
        data.put("totalFolders", folders.size());
        data.put("pendingWorkflows", workflows.values().stream().filter(workflow -> workflow.status == WorkflowStatus.PENDING).count());
        data.put("unreadNotifications", notifications.values().stream().filter(notification -> notification.userId == user.id && !notification.read).count());
        data.put("citizenApplications", applications.size());
        data.put("recentDocuments", recentDocuments);
        data.put("role", user.role.name());
        return data;
    }

    public synchronized Map<String, Object> downloadDocument(String token, long documentId) {
        User user = requireUser(token);
        Document document = requireDocument(documentId);
        ensureDocumentAccess(user, document);
        DocumentVersion version = versions.get(document.currentVersionId);
        if (version == null) {
            throw new HttpException(404, "Document version not found");
        }
        try {
            byte[] bytes = Files.readAllBytes(Paths.get(version.storagePath));
            return response(
                    "fileName", version.fileName,
                    "mimeType", version.mimeType,
                    "contentBase64", Base64.getEncoder().encodeToString(bytes)
            );
        } catch (IOException exception) {
            throw new HttpException(500, "Unable to read stored file");
        }
    }

    public synchronized Map<String, Object> getTraceability() {
        List<Map<String, Object>> matrix = new ArrayList<>();
        matrix.add(trace("REQ-01", "Intelligent search", "Document list filters and query text", "See listDocuments(query, fileType, folderId, from, to)"));
        matrix.add(trace("REQ-02", "General access folders", "Folder.generalAccess and folder-based access checks", "General folders are visible to all authenticated users"));
        matrix.add(trace("REQ-03", "Upload and duplicate detection", "uploadDocument and checksum comparison", "Rejects duplicate uploads with HTTP 409"));
        matrix.add(trace("REQ-04", "Version control", "DocumentVersion records", "addVersion creates a new immutable version"));
        matrix.add(trace("REQ-05", "Comments and collaboration", "Comment and Permission models", "shareDocument and addComment"));
        matrix.add(trace("REQ-06", "Approvals and rejections", "Workflow model", "submitForApproval and reviewWorkflow"));
        matrix.add(trace("REQ-07", "Superuser account management", "listUsers, createUser, updateUser", "IT_SUPERUSER role only"));
        matrix.add(trace("REQ-08", "Lock after failed attempts", "login() lockout flow", "Locks after five failures"));
        matrix.add(trace("REQ-09", "Reset email process", "startPasswordReset and completePasswordReset", "Returns demo token and updates password"));
        matrix.add(trace("REQ-10", "Recycle bin", "trashDocument and restoreDocument", "Soft delete and restoration"));
        matrix.add(trace("REQ-11", "Session timeout", "Session expiry in memory", "3-minute demo timeout configured"));
        matrix.add(trace("REQ-12", "Citizen portal", "CitizenApplication flow", "submitCitizenApplication and listCitizenApplications"));
        return response("items", matrix);
    }

    public synchronized byte[] rawDownload(String token, long documentId) {
        User user = requireUser(token);
        Document document = requireDocument(documentId);
        ensureDocumentAccess(user, document);
        DocumentVersion version = versions.get(document.currentVersionId);
        if (version == null) {
            throw new HttpException(404, "Document version not found");
        }
        try {
            return Files.readAllBytes(Paths.get(version.storagePath));
        } catch (IOException exception) {
            throw new HttpException(500, "Unable to read stored file");
        }
    }

    public synchronized String mimeType(long documentId) {
        Document document = documents.get(documentId);
        if (document == null || document.currentVersionId == null) {
            return "application/octet-stream";
        }
        DocumentVersion version = versions.get(document.currentVersionId);
        return version == null || version.mimeType == null ? "application/octet-stream" : version.mimeType;
    }

    public synchronized String fileName(long documentId) {
        Document document = documents.get(documentId);
        if (document == null || document.currentVersionId == null) {
            return "document.bin";
        }
        DocumentVersion version = versions.get(document.currentVersionId);
        return version == null || version.fileName == null ? "document.bin" : version.fileName;
    }

    public synchronized String resolveToken(String token) {
        return token;
    }

    public synchronized User currentUser(String token) {
        return requireUser(token);
    }

    private User createUser(String fullName, String email, Role role, String password) {
        User user = new User();
        user.id = ids.getAndIncrement();
        user.fullName = fullName;
        user.email = email.toLowerCase(Locale.ROOT);
        user.role = role;
        user.passwordHash = hashPassword(password);
        users.put(user.id, user);
        return user;
    }

    private Folder createFolder(String name, String description, boolean generalAccess, Long parentFolderId) {
        Folder folder = new Folder();
        folder.id = ids.getAndIncrement();
        folder.name = name;
        folder.description = description;
        folder.generalAccess = generalAccess;
        folder.parentFolderId = parentFolderId;
        folders.put(folder.id, folder);
        return folder;
    }

    private Document uploadDocumentInternal(User owner, long folderId, String title, String description, String fileName, String mimeType, byte[] bytes, boolean grantOwnerAccess) {
        Folder folder = folders.get(folderId);
        if (folder == null) {
            throw new HttpException(404, "Folder not found");
        }
        Document document = new Document();
        document.id = ids.getAndIncrement();
        document.title = title;
        document.description = description;
        document.folderId = folder.id;
        document.ownerUserId = owner.id;
        document.fileType = mimeType;
        document.checksum = checksum(bytes);
        documents.put(document.id, document);

        DocumentVersion version = addVersionInternal(document, owner, fileName, mimeType, bytes);
        document.currentVersionId = version.id;
        document.currentVersionNumber = version.versionNumber;
        document.checksum = version.checksum;
        document.updatedAt = Instant.now();
        if (grantOwnerAccess) {
            Permission permission = new Permission();
            permission.id = ids.getAndIncrement();
            permission.documentId = document.id;
            permission.userId = owner.id;
            permission.canView = true;
            permission.canEdit = true;
            permission.canComment = true;
            permission.canShare = true;
            permission.canApprove = true;
            permission.grantedByUserId = owner.id;
            permissions.put(permission.id, permission);
            document.permissionIds.add(permission.id);
        }
        return document;
    }

    private DocumentVersion addVersionInternal(Document document, User actor, String fileName, String mimeType, byte[] bytes) {
        int versionNumber = document.versionIds.size() + 1;
        DocumentVersion version = new DocumentVersion();
        version.id = ids.getAndIncrement();
        version.documentId = document.id;
        version.versionNumber = versionNumber;
        version.fileName = fileName;
        version.mimeType = mimeType;
        version.sizeBytes = bytes.length;
        version.checksum = checksum(bytes);
        version.createdByUserId = actor.id;
        version.storagePath = storeFile(document.id, versionNumber, fileName, bytes);
        versions.put(version.id, version);
        document.versionIds.add(version.id);
        document.currentVersionId = version.id;
        document.currentVersionNumber = versionNumber;
        document.checksum = version.checksum;
        document.fileType = mimeType;
        document.updatedAt = Instant.now();
        return version;
    }

    private String storeFile(long documentId, int versionNumber, String fileName, byte[] bytes) {
        try {
            Path documentDir = storageRoot.resolve("documents").resolve(String.valueOf(documentId));
            Files.createDirectories(documentDir);
            Path target = documentDir.resolve("v" + versionNumber + "-" + sanitizeFileName(fileName));
            Files.write(target, bytes);
            return target.toString();
        } catch (IOException exception) {
            throw new HttpException(500, "Unable to persist uploaded file");
        }
    }

    private Comment addCommentInternal(long documentId, User actor, String body) {
        Comment comment = new Comment();
        comment.id = ids.getAndIncrement();
        comment.documentId = documentId;
        comment.authorUserId = actor.id;
        comment.body = body;
        comments.put(comment.id, comment);
        Document document = requireDocument(documentId);
        document.commentIds.add(comment.id);
        document.updatedAt = Instant.now();
        return comment;
    }

    private Workflow submitForApprovalInternal(User actor, long documentId, String notes) {
        Document document = requireDocument(documentId);
        Workflow workflow = new Workflow();
        workflow.id = ids.getAndIncrement();
        workflow.documentId = documentId;
        workflow.requestedByUserId = actor.id;
        workflow.notes = notes;
        workflow.status = WorkflowStatus.PENDING;
        workflow.reviewerUserId = resolveReviewer().id;
        workflows.put(workflow.id, workflow);
        document.workflowId = workflow.id;
        document.status = DocumentStatus.IN_REVIEW;
        document.updatedAt = Instant.now();
        addNotification(workflow.reviewerUserId, NotificationType.APPROVAL_REQUIRED, "Approval required", document.title + " is awaiting your decision.", "Workflow", workflow.id);
        createAudit(actor.id, "SUBMIT_APPROVAL", "document", document.id, notes);
        return workflow;
    }

    private User resolveReviewer() {
        return users.values().stream()
                .filter(user -> user.role == Role.REVIEWER || user.role == Role.IT_SUPERUSER)
                .findFirst()
                .orElseThrow(() -> new HttpException(500, "No reviewer account exists"));
    }

    private void notifyCollaborators(Document document, NotificationType type, String title, String message, long exceptUserId) {
        for (Permission permission : permissions.values()) {
            if (permission.documentId == document.id && permission.userId != exceptUserId) {
                addNotification(permission.userId, type, title, message, "Document", document.id);
            }
        }
    }

    private Notification addNotification(long userId, NotificationType type, String title, String message, String relatedType, Long relatedId) {
        Notification notification = new Notification();
        notification.id = ids.getAndIncrement();
        notification.userId = userId;
        notification.type = type;
        notification.title = title;
        notification.message = message;
        notification.relatedType = relatedType;
        notification.relatedId = relatedId;
        notifications.put(notification.id, notification);
        return notification;
    }

    private void createAudit(long actorUserId, String action, String entityType, Long entityId, String detail) {
        AuditLog auditLog = new AuditLog();
        auditLog.id = ids.getAndIncrement();
        auditLog.actorUserId = actorUserId;
        auditLog.action = action;
        auditLog.entityType = entityType;
        auditLog.entityId = entityId;
        auditLog.detail = detail;
        auditLogs.put(auditLog.id, auditLog);
    }

    private User requireUser(String token) {
        Session session = sessions.get(token);
        if (session == null) {
            throw new HttpException(401, "Authentication required");
        }
        if (session.expiresAt.isBefore(Instant.now())) {
            sessions.remove(token);
            throw new HttpException(401, "Session expired");
        }
        session.expiresAt = Instant.now().plus(Duration.ofMinutes(sessionTimeoutMinutes));
        User user = users.get(session.userId);
        if (user == null) {
            throw new HttpException(401, "Authentication required");
        }
        return user;
    }

    private void requireRole(String token, Role role) {
        User user = requireUser(token);
        if (user.role != role) {
            throw new HttpException(403, "Forbidden");
        }
    }

    private boolean canViewDocument(User user, Document document) {
        if (isAdmin(user) || user.role == Role.REVIEWER) {
            return true;
        }
        if (document.ownerUserId == user.id) {
            return true;
        }
        Folder folder = folders.get(document.folderId);
        if (folder != null && folder.generalAccess) {
            return true;
        }
        Permission permission = findPermission(document.id, user.id);
        return permission != null && permission.canView;
    }

    private void ensureDocumentAccess(User user, Document document) {
        if (!canViewDocument(user, document)) {
            throw new HttpException(403, "You do not have access to this document");
        }
    }

    private void ensureDocumentEditAccess(User user, Document document) {
        if (isAdmin(user) || document.ownerUserId == user.id) {
            return;
        }
        Permission permission = findPermission(document.id, user.id);
        if (permission != null && permission.canEdit) {
            return;
        }
        throw new HttpException(403, "You do not have edit rights to this document");
    }

    private void ensureDocumentCommentAccess(User user, Document document) {
        if (canViewDocument(user, document)) {
            return;
        }
        Permission permission = findPermission(document.id, user.id);
        if (permission != null && permission.canComment) {
            return;
        }
        throw new HttpException(403, "You do not have comment rights to this document");
    }

    private boolean isAdmin(User user) {
        return user.role == Role.IT_SUPERUSER;
    }

    private Permission findPermission(long documentId, long userId) {
        return permissions.values().stream()
                .filter(permission -> permission.documentId == documentId && permission.userId == userId)
                .findFirst()
                .orElse(null);
    }

    private User findUserByEmail(String email) {
        if (email == null) {
            return null;
        }
        String normalized = email.toLowerCase(Locale.ROOT);
        return users.values().stream()
                .filter(user -> user.email.equals(normalized))
                .findFirst()
                .orElse(null);
    }

    private User requireUserById(long userId) {
        User user = users.get(userId);
        if (user == null) {
            throw new HttpException(404, "User not found");
        }
        return user;
    }

    private Document requireDocument(long documentId) {
        Document document = documents.get(documentId);
        if (document == null) {
            throw new HttpException(404, "Document not found");
        }
        return document;
    }

    private Workflow requireWorkflow(long workflowId) {
        Workflow workflow = workflows.get(workflowId);
        if (workflow == null) {
            throw new HttpException(404, "Workflow not found");
        }
        return workflow;
    }

    private DocumentVersion requireDocumentVersion(long versionId) {
        DocumentVersion version = versions.get(versionId);
        if (version == null) {
            throw new HttpException(404, "Document version not found");
        }
        return version;
    }

    private String hashPassword(String password) {
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest(password.getBytes(StandardCharsets.UTF_8));
            StringBuilder builder = new StringBuilder();
            for (byte value : hash) {
                builder.append(String.format(Locale.ROOT, "%02x", value));
            }
            return builder.toString();
        } catch (NoSuchAlgorithmException exception) {
            throw new IllegalStateException(exception);
        }
    }

    private String checksum(byte[] bytes) {
        return hashPassword(Base64.getEncoder().encodeToString(bytes));
    }

    private String sanitizeFileName(String fileName) {
        return fileName.replaceAll("[^a-zA-Z0-9._-]", "_");
    }

    private boolean matches(Document document, String search) {
        String haystack = (document.title + " " + nullToEmpty(document.description) + " " + nullToEmpty(document.fileType)).toLowerCase(Locale.ROOT);
        return haystack.contains(search);
    }

    private String normalize(String value) {
        if (value == null || value.isBlank()) {
            return null;
        }
        return value.trim().toLowerCase(Locale.ROOT);
    }

    private String nullToEmpty(String value) {
        return value == null ? "" : value;
    }

    private String stringOrDefault(String value, String defaultValue) {
        return value == null || value.isBlank() ? defaultValue : value;
    }

    private String requiredString(Map<String, Object> body, String key) {
        String value = stringValue(body, key, null);
        if (value == null || value.isBlank()) {
            throw new HttpException(400, "Missing required field: " + key);
        }
        return value;
    }

    private String stringValue(Map<String, Object> body, String key, String defaultValue) {
        Object value = body.get(key);
        return value == null ? defaultValue : String.valueOf(value);
    }

    private boolean booleanValue(Map<String, Object> body, String key, boolean defaultValue) {
        Object value = body.get(key);
        if (value == null) {
            return defaultValue;
        }
        if (value instanceof Boolean bool) {
            return bool;
        }
        return Boolean.parseBoolean(String.valueOf(value));
    }

    private Long optionalLong(Map<String, Object> body, String key) {
        Object value = body.get(key);
        if (value == null || String.valueOf(value).isBlank()) {
            return null;
        }
        return Long.parseLong(String.valueOf(value));
    }

    private Long parseOptionalLong(String value) {
        if (value == null || value.isBlank()) {
            return null;
        }
        return Long.parseLong(value);
    }

    @SuppressWarnings("unchecked")
    private Map<String, Object> asMap(Object value) {
        if (value instanceof Map<?, ?> map) {
            Map<String, Object> result = new LinkedHashMap<>();
            for (Map.Entry<?, ?> entry : map.entrySet()) {
                result.put(String.valueOf(entry.getKey()), entry.getValue());
            }
            return result;
        }
        throw new HttpException(400, "Expected nested object");
    }

    private Map<String, Object> response(Object... keyValues) {
        Map<String, Object> result = new LinkedHashMap<>();
        for (int index = 0; index < keyValues.length; index += 2) {
            result.put(String.valueOf(keyValues[index]), keyValues[index + 1]);
        }
        return result;
    }

    private Map<String, Object> userView(User user) {
        return response(
                "id", user.id,
                "fullName", user.fullName,
                "email", user.email,
                "role", user.role.name(),
                "active", user.active,
                "failedAttempts", user.failedAttempts,
                "lockedUntil", user.lockedUntil == null ? null : user.lockedUntil.toString(),
                "createdAt", user.createdAt == null ? null : user.createdAt.toString(),
                "lastLoginAt", user.lastLoginAt == null ? null : user.lastLoginAt.toString()
        );
    }

    private Map<String, Object> folderView(Folder folder) {
        return response(
                "id", folder.id,
                "name", folder.name,
                "description", folder.description,
                "generalAccess", folder.generalAccess,
                "parentFolderId", folder.parentFolderId,
                "createdAt", folder.createdAt.toString()
        );
    }

    private Map<String, Object> documentSummary(Document document, User currentUser) {
        Folder folder = folders.get(document.folderId);
        User owner = users.get(document.ownerUserId);
        return response(
                "id", document.id,
                "title", document.title,
                "description", document.description,
                "folderId", document.folderId,
                "folderName", folder == null ? null : folder.name,
                "ownerName", owner == null ? null : owner.fullName,
                "status", document.status.name(),
                "fileType", document.fileType,
                "checksum", document.checksum,
                "currentVersionNumber", document.currentVersionNumber,
                "deleted", document.deleted,
                "updatedAt", document.updatedAt.toString(),
                "canEdit", canEditDocument(currentUser, document),
                "canView", canViewDocument(currentUser, document)
        );
    }

    private boolean canEditDocument(User user, Document document) {
        if (isAdmin(user) || document.ownerUserId == user.id) {
            return true;
        }
        Permission permission = findPermission(document.id, user.id);
        return permission != null && permission.canEdit;
    }

    private Map<String, Object> documentDetail(Document document, User currentUser) {
        Map<String, Object> data = documentSummary(document, currentUser);
        data.put("workflow", document.workflowId == null ? null : workflowView(requireWorkflow(document.workflowId)));
        data.put("versions", document.versionIds.stream().map(versionId -> versionView(requireDocumentVersion(versionId))).toList());
        data.put("comments", document.commentIds.stream().map(commentId -> commentView(comments.get(commentId))).toList());
        data.put("permissions", document.permissionIds.stream().map(permissionId -> permissionView(permissions.get(permissionId))).toList());
        return data;
    }

    private Map<String, Object> versionView(DocumentVersion version) {
        User createdBy = users.get(version.createdByUserId);
        return response(
                "id", version.id,
                "documentId", version.documentId,
                "versionNumber", version.versionNumber,
                "fileName", version.fileName,
                "mimeType", version.mimeType,
                "checksum", version.checksum,
                "sizeBytes", version.sizeBytes,
                "createdBy", createdBy == null ? null : createdBy.fullName,
                "createdAt", version.createdAt.toString()
        );
    }

    private Map<String, Object> commentView(Comment comment) {
        User author = users.get(comment.authorUserId);
        return response(
                "id", comment.id,
                "documentId", comment.documentId,
                "authorName", author == null ? null : author.fullName,
                "body", comment.body,
                "createdAt", comment.createdAt.toString()
        );
    }

    private Map<String, Object> permissionView(Permission permission) {
        User target = users.get(permission.userId);
        return response(
                "id", permission.id,
                "documentId", permission.documentId,
                "userId", permission.userId,
                "userName", target == null ? null : target.fullName,
                "email", target == null ? null : target.email,
                "canView", permission.canView,
                "canEdit", permission.canEdit,
                "canComment", permission.canComment,
                "canShare", permission.canShare,
                "canApprove", permission.canApprove,
                "grantedAt", permission.grantedAt.toString()
        );
    }

    private Map<String, Object> workflowView(Workflow workflow) {
        User requester = users.get(workflow.requestedByUserId);
        User reviewer = workflow.reviewerUserId == null ? null : users.get(workflow.reviewerUserId);
        Document document = documents.get(workflow.documentId);
        return response(
                "id", workflow.id,
                "documentId", workflow.documentId,
                "documentTitle", document == null ? null : document.title,
                "status", workflow.status.name(),
                "currentStep", workflow.currentStep,
                "requestedBy", requester == null ? null : requester.fullName,
                "reviewer", reviewer == null ? null : reviewer.fullName,
                "notes", workflow.notes,
                "createdAt", workflow.createdAt.toString(),
                "updatedAt", workflow.updatedAt.toString()
        );
    }

    private Map<String, Object> notificationView(Notification notification) {
        return response(
                "id", notification.id,
                "userId", notification.userId,
                "type", notification.type.name(),
                "title", notification.title,
                "message", notification.message,
                "relatedType", notification.relatedType,
                "relatedId", notification.relatedId,
                "read", notification.read,
                "silenced", notification.silenced,
                "createdAt", notification.createdAt.toString()
        );
    }

    private Map<String, Object> applicationView(CitizenApplication application) {
        Document attachment = application.attachedDocumentId == null ? null : documents.get(application.attachedDocumentId);
        return response(
                "id", application.id,
                "applicationNumber", application.applicationNumber,
                "applicationType", application.applicationType,
                "fullName", application.fullName,
                "email", application.email,
                "idNumber", application.idNumber,
                "phoneNumber", application.phoneNumber,
                "notes", application.notes,
                "status", application.status.name(),
                "attachedDocumentId", application.attachedDocumentId,
                "attachedDocumentTitle", attachment == null ? null : attachment.title,
                "submittedAt", Instant.ofEpochMilli(application.submittedAt).toString(),
                "reviewedByUserId", application.reviewedByUserId,
                "reviewedAt", application.reviewedAt == null ? null : Instant.ofEpochMilli(application.reviewedAt).toString()
        );
    }

    private Map<String, Object> trace(String requirement, String designElement, String implementation, String justification) {
        return response(
                "requirement", requirement,
                "designElement", designElement,
                "implementation", implementation,
                "justification", justification
        );
    }

    private static final class Session {
        private final long userId;
        private Instant expiresAt;

        private Session(long userId, Instant expiresAt) {
            this.userId = userId;
            this.expiresAt = expiresAt;
        }
    }
}