using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class RequiredDocumentRule
    {
        [Key]
        public int RequiredDocumentRuleId { get; set; }

        [Required]
        [ForeignKey(nameof(ApplicationType))]
        public int ApplicationTypeId { get; set; }

        public ApplicationType ApplicationType { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(DocumentType))]
        public int DocumentTypeId { get; set; }

        public DocumentType DocumentType { get; set; } = null!;

        public bool IsRequired { get; set; } = true;

        public bool AcceptsMultiple { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
