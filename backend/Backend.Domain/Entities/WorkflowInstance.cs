using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class WorkflowInstance
    {
        [Key]
        public int WorkflowInstanceId { get; set; }

        [Required]
        [ForeignKey(nameof(Application))]
        public int ApplicationId { get; set; }

        public Application Application { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(CurrentWorkflowStage))]
        public int CurrentWorkflowStageId { get; set; }

        public WorkflowStage CurrentWorkflowStage { get; set; } = null!;

        [ForeignKey(nameof(AssignedPractitioner))]
        public int? AssignedPractitionerId { get; set; }

        public Practitioner? AssignedPractitioner { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? LastMovedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public ICollection<WorkflowStageHistory> StageHistory { get; set; } = new List<WorkflowStageHistory>();
    }
}
