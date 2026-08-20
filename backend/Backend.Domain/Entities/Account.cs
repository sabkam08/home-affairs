using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.Domain.Entities
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public AccountRole Role { get; set; }

        [Required]
        public AccountStatus Status { get; set; }

        public bool IsEmailVerified { get; set; }

        public int FailedLoginAttempts { get; set; }

        public DateTime? LockedUntil { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Citizen? CitizenProfile { get; set; }

        public Practitioner? PractitionerProfile { get; set; }

        public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();

        public ICollection<DocumentVersion> UploadedDocumentVersions { get; set; } = new List<DocumentVersion>();

        public ICollection<DocumentComment> WrittenComments { get; set; } = new List<DocumentComment>();

        public ICollection<DocumentPermission> GrantedDocumentPermissions { get; set; } = new List<DocumentPermission>();

        public ICollection<DocumentPermission> ReceivedDocumentPermissions { get; set; } = new List<DocumentPermission>();

        public ICollection<WorkflowStageHistory> WorkflowActions { get; set; } = new List<WorkflowStageHistory>();

        public ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();

        public ICollection<Notification> TriggeredNotifications { get; set; } = new List<Notification>();

        public ICollection<Folder> OwnedFolders { get; set; } = new List<Folder>();

        public ICollection<FolderPermission> GrantedFolderPermissions { get; set; } = new List<FolderPermission>();

        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    }
}
