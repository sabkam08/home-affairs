using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Domain.Entities
{
    public class Citizen
    {
        [Key]
        public int CitizenId { get; set; }

        [Required]
        [ForeignKey(nameof(Account))]
        public int AccountId { get; set; }

        public Account Account { get; set; } = null!;

        [MaxLength(30)]
        public string? NationalIdentityNumber { get; set; }

        [MaxLength(200)]
        public string? GivenNames { get; set; }

        [MaxLength(200)]
        public string? Surname { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
