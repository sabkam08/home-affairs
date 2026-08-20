using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class DocumentVersion
    {
        [Key]
        public int DocumentVersionId { get; set; }

        [Required]
        [ForeignKey(nameof(Document))]
        public int DocumentId { get; set; }

        public Document Document { get; set; } = null!;

        public int VersionNumber { get; set; }

        [Required]
        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(260)]
        public string? StoragePath { get; set; }

        [Required]
        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [MaxLength(128)]
        public string? ContentHash { get; set; }

        [Required]
        [ForeignKey(nameof(UploadedByAccount))]
        public int UploadedByAccountId { get; set; }

        public Account UploadedByAccount { get; set; } = null!;

        public bool IsCurrent { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
