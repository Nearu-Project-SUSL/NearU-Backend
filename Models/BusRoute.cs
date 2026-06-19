using System.ComponentModel.DataAnnotations;

namespace NearU.Api.Models
{
    public class BusRoute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RouteName { get; set; } = string.Empty;   // e.g. "Belihuloya - Pelmadulla"

        [Required]
        [MaxLength(100)]
        public string StartPoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EndPoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string DepartureTime { get; set; } = string.Empty;  // stored as "HH:mm", simple string for a timetable

        [MaxLength(10)]
        public string? ArrivalTime { get; set; }

        [MaxLength(50)]
        public string? BusNumber { get; set; }     // optional route/bus identifier, e.g. "EP-1234"

        [MaxLength(300)]
        public string? Notes { get; set; }         // e.g. "Daily except Sunday"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}