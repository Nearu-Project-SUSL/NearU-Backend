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

        // Type of accommodation: Boarding, Annex, Apartment
        public string? Type { get; set; }

        // Distance from campus in kilometers
        public decimal DistanceKm { get; set; } = 0;

        // Monthly rent in LKR
        public decimal MonthlyRent { get; set; } = 0;

        // Number of available beds
        public int AvailableBeds { get; set; } = 0;

        // Comma-separated list of amenities
        public string? Amenities { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for Accommodation Items
        public virtual ICollection<AccommodationItem> AccommodationItems { get; set; } = new List<AccommodationItem>();
    }
}
