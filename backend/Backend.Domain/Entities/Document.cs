using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty;

        [MaxLength(260)]
        public string? OriginalFileName { get; set; }

        [MaxLength(260)]
        public string? StoragePath { get; set; }

        [MaxLength(128)]
        public string? ContentHash { get; set; }

        [ForeignKey(nameof(Application))]
        public int? ApplicationId { get; set; }

        public Application? Application { get; set; }

        [ForeignKey(nameof(Folder))]
        public int? FolderId { get; set; }

        public Folder? Folder { get; set; }

        [ForeignKey(nameof(DocumentType))]
        public int? DocumentTypeId { get; set; }

        public DocumentType? DocumentType { get; set; }

        [Required]
        [ForeignKey(nameof(UploadedByAccount))]
        public int UploadedByAccountId { get; set; }

        public Account UploadedByAccount { get; set; } = null!;

        [ForeignKey(nameof(CurrentVersion))]
        public int? CurrentVersionId { get; set; }

        public DocumentVersion? CurrentVersion { get; set; }

        public bool IsEncrypted { get; set; }

        public bool IsDuplicate { get; set; }

        public bool IsInRecycleBin { get; set; }

        public System.DateTime UploadedAt { get; set; }

        public System.DateTime? UpdatedAt { get; set; }

        public System.DateTime? DeletedAt { get; set; }

        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

        public ICollection<DocumentPermission> Permissions { get; set; } = new List<DocumentPermission>();

        public ICollection<DocumentComment> Comments { get; set; } = new List<DocumentComment>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
