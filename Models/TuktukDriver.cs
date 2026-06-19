using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NearU_Backend_Revised.Models
{
    public class TukTukDriver
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? OperatingArea { get; set; }   // e.g. "Belihuloya - Campus"

        [MaxLength(500)]
        public string? Notes { get; set; }            // any extra info admin wants to add

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}