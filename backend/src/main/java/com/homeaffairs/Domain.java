package com.homeaffairs;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;

public final class Domain {

    private Domain() {
    }

    public enum Role {
        IT_SUPERUSER,
        REVIEWER,
        EMPLOYEE,
        CITIZEN
    }

    public enum DocumentStatus {
        DRAFT,
        IN_REVIEW,
        APPROVED,
        REJECTED,
        TRASHED
    }

    public enum WorkflowStatus {
        PENDING,
        APPROVED,
        REJECTED
    }

    public enum NotificationType {
        DOCUMENT_UPDATED,
        DOCUMENT_SHARED,
        APPROVAL_REQUIRED,
        APPROVED,
        REJECTED,
        PASSWORD_RESET,
        APPLICATION_SUBMITTED,
        USER_ADMIN
    }

    public enum ApplicationStatus {
        SUBMITTED,
        UNDER_REVIEW,
        APPROVED,
        REJECTED
    }

    public static class User {
        public long id;
        public String fullName;
        public String email;
        public String passwordHash;
        public Role role;
        public boolean active = true;
        public int failedAttempts;
        public Instant lockedUntil;
        public Instant createdAt = Instant.now();
        public Instant lastLoginAt;
    }

    public static class Folder {
        public long id;
        public String name;
        public String description;
        public boolean generalAccess;
        public Long parentFolderId;
        public Instant createdAt = Instant.now();
    }

    public static class DocumentVersion {
        public long id;
        public long documentId;
        public int versionNumber;
        public String fileName;
        public String mimeType;
        public String storagePath;
        public String checksum;
        public long sizeBytes;
        public long createdByUserId;
        public Instant createdAt = Instant.now();
    }

    public static class Comment {
        public long id;
        public long documentId;
        public long authorUserId;
        public String body;
        public Instant createdAt = Instant.now();
    }

    public static class Permission {
        public long id;
        public long documentId;
        public long userId;
        public boolean canView;
        public boolean canEdit;
        public boolean canComment;
        public boolean canShare;
        public boolean canApprove;
        public long grantedByUserId;
        public Instant grantedAt = Instant.now();
    }

    public static class Workflow {
        public long id;
        public long documentId;
        public WorkflowStatus status = WorkflowStatus.PENDING;
        public String currentStep = "Review";
        public long requestedByUserId;
        public Long reviewerUserId;
        public String notes;
        public Instant createdAt = Instant.now();
        public Instant updatedAt = Instant.now();
    }

    public static class Notification {
        public long id;
        public long userId;
        public NotificationType type;
        public String title;
        public String message;
        public String relatedType;
        public Long relatedId;
        public boolean read;
        public boolean silenced;
        public Instant createdAt = Instant.now();
    }

    public static class CitizenApplication {
        public long id;
        public String applicationNumber;
        public String applicationType;
        public String fullName;
        public String email;
        public String idNumber;
        public String phoneNumber;
        public String notes;
        public ApplicationStatus status = ApplicationStatus.SUBMITTED;
        public long submittedByUserId;
        public Long attachedDocumentId;
        public long submittedAt = Instant.now().toEpochMilli();
        public Long reviewedByUserId;
        public Long reviewedAt;
    }

    public static class ResetToken {
        public String token;
        public long userId;
        public Instant expiresAt;
        public Instant usedAt;
    }

    public static class AuditLog {
        public long id;
        public long actorUserId;
        public String action;
        public String entityType;
        public Long entityId;
        public String detail;
        public Instant createdAt = Instant.now();
    }

    public static class Document {
        public long id;
        public String title;
        public String description;
        public long folderId;
        public long ownerUserId;
        public DocumentStatus status = DocumentStatus.DRAFT;
        public int currentVersionNumber = 1;
        public Long currentVersionId;
        public String checksum;
        public String fileType;
        public boolean deleted;
        public Instant deletedAt;
        public Instant createdAt = Instant.now();
        public Instant updatedAt = Instant.now();
        public final List<Long> versionIds = new ArrayList<>();
        public final List<Long> commentIds = new ArrayList<>();
        public final List<Long> permissionIds = new ArrayList<>();
        public Long workflowId;
    }
}