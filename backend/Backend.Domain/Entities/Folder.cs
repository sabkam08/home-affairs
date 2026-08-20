using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class Folder
    {
        [Key]
        public int FolderId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [Required]
        public FolderScope Scope { get; set; }

        [ForeignKey(nameof(ParentFolder))]
        public int? ParentFolderId { get; set; }

        public Folder? ParentFolder { get; set; }

        [ForeignKey(nameof(BranchUnit))]
        public int? BranchUnitId { get; set; }

        public BranchUnit? BranchUnit { get; set; }

        [ForeignKey(nameof(ApplicationType))]
        public int? ApplicationTypeId { get; set; }

        public ApplicationType? ApplicationType { get; set; }

        [ForeignKey(nameof(WorkflowStage))]
        public int? WorkflowStageId { get; set; }

        public WorkflowStage? WorkflowStage { get; set; }

        [ForeignKey(nameof(OwnerAccount))]
        public int? OwnerAccountId { get; set; }

        public Account? OwnerAccount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsArchived { get; set; }

        public ICollection<Folder> ChildFolders { get; set; } = new List<Folder>();

        public ICollection<Document> Documents { get; set; } = new List<Document>();

        public ICollection<FolderPermission> FolderPermissions { get; set; } = new List<FolderPermission>();
    }
}
