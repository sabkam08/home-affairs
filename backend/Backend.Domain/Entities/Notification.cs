using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        [ForeignKey(nameof(RecipientAccount))]
        public int RecipientAccountId { get; set; }

        public Account RecipientAccount { get; set; } = null!;

        [ForeignKey(nameof(TriggeredByAccount))]
        public int? TriggeredByAccountId { get; set; }

        public Account? TriggeredByAccount { get; set; }

        [ForeignKey(nameof(Application))]
        public int? ApplicationId { get; set; }

        public Application? Application { get; set; }

        [ForeignKey(nameof(Document))]
        public int? DocumentId { get; set; }

        public Document? Document { get; set; }

        [ForeignKey(nameof(Comment))]
        public int? CommentId { get; set; }

        public DocumentComment? Comment { get; set; }

        [Required]
        public NotificationType NotificationType { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }
    }
}
