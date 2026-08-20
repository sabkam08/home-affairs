using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class FolderPermission
    {
        [Key]
        public int FolderPermissionId { get; set; }

        [Required]
        [ForeignKey(nameof(Folder))]
        public int FolderId { get; set; }

        public Folder Folder { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(BranchUnit))]
        public int BranchUnitId { get; set; }

        public BranchUnit BranchUnit { get; set; } = null!;

        public bool CanView { get; set; } = true;

        public bool CanAdd { get; set; }

        public bool CanEdit { get; set; }

        public bool CanDelete { get; set; }

        public bool CanApprove { get; set; }

        public DateTime GrantedAt { get; set; }

        [ForeignKey(nameof(GrantedByAccount))]
        public int? GrantedByAccountId { get; set; }

        public Account? GrantedByAccount { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
