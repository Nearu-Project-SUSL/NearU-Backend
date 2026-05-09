using System.ComponentModel.DataAnnotations;

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

        [Required]
        public double PickupLat { get; set; }

        [Required]
        public double PickupLon { get; set; }

        [Required]
        public double DropoffLat { get; set; }

        [Required]
        public double DropoffLon { get; set; }

        [MaxLength(10)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Completed, Expired

        [MaxLength(4)]
        public string? OTP { get; set; }

        public decimal? Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

 