using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class DocumentPermission
    {
        [Key]
        public int DocumentPermissionId { get; set; }

        [Required]
        [ForeignKey(nameof(Document))]
        public int DocumentId { get; set; }

        public Document Document { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(Account))]
        public int AccountId { get; set; }

        public Account Account { get; set; } = null!;

        [Required]
        public DocumentPermissionLevel PermissionLevel { get; set; }

        [Required]
        [ForeignKey(nameof(GrantedByAccount))]
        public int GrantedByAccountId { get; set; }

        public Account GrantedByAccount { get; set; } = null!;

        public DateTime GrantedAt { get; set; }

        public bool IsMuted { get; set; }
    }
}
