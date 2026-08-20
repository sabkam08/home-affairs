using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.Domain.Entities
{
    public class WorkflowStage
    {
        [Key]
        public int WorkflowStageId { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public int SequenceOrder { get; set; }

        public bool IsFinalStage { get; set; }

        public ICollection<WorkflowInstance> CurrentWorkflowInstances { get; set; } = new List<WorkflowInstance>();

        public ICollection<WorkflowStageHistory> FromStageHistory { get; set; } = new List<WorkflowStageHistory>();

        public ICollection<WorkflowStageHistory> ToStageHistory { get; set; } = new List<WorkflowStageHistory>();

        public ICollection<Folder> Folders { get; set; } = new List<Folder>();
    }
}
