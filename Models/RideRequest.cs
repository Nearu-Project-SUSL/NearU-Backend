using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries; // Point type comes from here

namespace NearU_Backend_Revised.Models
{
    public class RideRequest
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StudentId { get; set; }

        public Guid? RiderId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ServiceType { get; set; } = string.Empty;

        public string? Details { get; set; }

        // Point stores both lat and lon together as a PostGIS geography column
        // SRID 4326 = standard GPS coordinate system (WGS84)
        [Required]
        public Point PickupLocation {get; set;} = null!;
        [Required]
        public Point DropoffLocation {get; set;} = null!;

        [MaxLength(10)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Completed, Expired

        [MaxLength(4)]
        public string? OTP { get; set; }

        public decimal? Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

 