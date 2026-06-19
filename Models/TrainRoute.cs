using System.ComponentModel.DataAnnotations;

namespace NearU.Api.Models
{
    public class TrainRoute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RouteName { get; set; } = string.Empty;  // e.g. "Colombo - Badulla"

        [Required]
        [MaxLength(100)]
        public string StartStation { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EndStation { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string DepartureTime { get; set; } = string.Empty;  // "HH:mm"

        [MaxLength(10)]
        public string? ArrivalTime { get; set; }

        [MaxLength(50)]
        public string? TrainName { get; set; }      // e.g. "Udarata Menike"

        [MaxLength(300)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}