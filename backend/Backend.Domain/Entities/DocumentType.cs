using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.Domain.Entities
{
    public class DocumentType
    {
        [Key]
        public int DocumentTypeId { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? AllowedExtensions { get; set; }

        public ICollection<Document> Documents { get; set; } = new List<Document>();

        public ICollection<RequiredDocumentRule> RequiredDocumentRules { get; set; } = new List<RequiredDocumentRule>();
    }
}
