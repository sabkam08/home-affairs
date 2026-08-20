using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class WorkflowStageHistory
    {
        [Key]
        public int WorkflowStageHistoryId { get; set; }

        [Required]
        [ForeignKey(nameof(WorkflowInstance))]
        public int WorkflowInstanceId { get; set; }

        public WorkflowInstance WorkflowInstance { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(FromWorkflowStage))]
        public int FromWorkflowStageId { get; set; }

        public WorkflowStage FromWorkflowStage { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(ToWorkflowStage))]
        public int ToWorkflowStageId { get; set; }

        public WorkflowStage ToWorkflowStage { get; set; } = null!;

        [ForeignKey(nameof(ActionedByPractitioner))]
        public int? ActionedByPractitionerId { get; set; }

        public Practitioner? ActionedByPractitioner { get; set; }

        [Required]
        public WorkflowActionType ActionType { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public DateTime ActionedAt { get; set; }
    }
}
