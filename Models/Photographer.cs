using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NearU_Backend_Revised.Models
{
    public class Photographer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Bio { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRatePerHour { get; set; }

        [Required]
        [MaxLength(150)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The Business user who owns/manages this photographer profile.
        /// </summary>
        public string? OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public virtual User? Owner { get; set; }

        public virtual ICollection<PhotographyPackage> Packages { get; set; } = new List<PhotographyPackage>();
    }
}
