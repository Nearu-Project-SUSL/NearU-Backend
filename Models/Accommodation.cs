using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace NearU_Backend_Revised.Models
{
    public class Accommodation
    {
        [Key]
        public string Id { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Address { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? PhotoUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for Accommodation Items
        public virtual ICollection<AccommodationItem> AccommodationItems { get; set; } = new List<AccommodationItem>();
    }
}
