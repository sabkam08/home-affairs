using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class Practitioner
    {
        [Key]
        public int PractitionerId { get; set; }

        [Required]
        [ForeignKey(nameof(Account))]
        public int AccountId { get; set; }

        public Account Account { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(BranchUnit))]
        public int BranchUnitId { get; set; }

        public BranchUnit BranchUnit { get; set; } = null!;

        [MaxLength(40)]
        public string? EmployeeNumber { get; set; }

        [MaxLength(200)]
        public string? GivenNames { get; set; }

        [MaxLength(200)]
        public string? Surname { get; set; }

        [MaxLength(120)]
        public string? JobTitle { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Application> AssignedApplications { get; set; } = new List<Application>();

        public ICollection<WorkflowStageHistory> ActionHistory { get; set; } = new List<WorkflowStageHistory>();

        public ICollection<DocumentPermission> GrantedDocumentPermissions { get; set; } = new List<DocumentPermission>();

        public ICollection<FolderPermission> GrantedFolderPermissions { get; set; } = new List<FolderPermission>();
    }
}
