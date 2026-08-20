using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class Application
    {
        [Key]
        public int ApplicationId { get; set; }

        [Required]
        [ForeignKey(nameof(Citizen))]
        public int CitizenId { get; set; }

        public Citizen Citizen { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(ApplicationType))]
        public int ApplicationTypeId { get; set; }

        public ApplicationType ApplicationType { get; set; } = null!;

        [ForeignKey(nameof(BranchUnit))]
        public int? ProcessingBranchUnitId { get; set; }

        public BranchUnit? ProcessingBranchUnit { get; set; }

        [ForeignKey(nameof(Practitioner))]
        public int? AssignedPractitionerId { get; set; }

        public Practitioner? AssignedPractitioner { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Required]
        public ApplicationStatus Status { get; set; }

        public DateTime SubmittedAt { get; set; }

        public DateTime? LastUpdatedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime? RejectedAt { get; set; }

        public DateTime? CollectionDate { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        public WorkflowInstance? WorkflowInstance { get; set; }

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
