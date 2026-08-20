using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class BranchUnit
    {
        [Key]
        public int BranchUnitId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [ForeignKey(nameof(ParentBranchUnit))]
        public int? ParentBranchUnitId { get; set; }

        public BranchUnit? ParentBranchUnit { get; set; }

        public ICollection<BranchUnit> ChildBranchUnits { get; set; } = new List<BranchUnit>();

        public ICollection<Practitioner> Practitioners { get; set; } = new List<Practitioner>();

        public ICollection<Application> Applications { get; set; } = new List<Application>();

        public ICollection<Folder> Folders { get; set; } = new List<Folder>();

        public ICollection<FolderPermission> FolderPermissions { get; set; } = new List<FolderPermission>();
    }
}
