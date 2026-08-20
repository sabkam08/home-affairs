using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class DocumentComment
    {
        [Key]
        public int DocumentCommentId { get; set; }

        [Required]
        [ForeignKey(nameof(Document))]
        public int DocumentId { get; set; }

        public Document Document { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(AuthorAccount))]
        public int AuthorAccountId { get; set; }

        public Account AuthorAccount { get; set; } = null!;

        [ForeignKey(nameof(ParentComment))]
        public int? ParentCommentId { get; set; }

        public DocumentComment? ParentComment { get; set; }

        [Required]
        [MaxLength(2000)]
        public string CommentText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsResolved { get; set; }

        public ICollection<DocumentComment> Replies { get; set; } = new List<DocumentComment>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
