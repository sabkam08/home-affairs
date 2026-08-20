using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.Domain.Entities
{
    public class ApplicationType
    {
        [Key]
        public int ApplicationTypeId { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Application> Applications { get; set; } = new List<Application>();

        public ICollection<RequiredDocumentRule> RequiredDocumentRules { get; set; } = new List<RequiredDocumentRule>();

        public ICollection<Folder> Folders { get; set; } = new List<Folder>();
    }
}
