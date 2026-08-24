using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NearU_Backend_Revised.Models
{
    public class AccommodationItem
    {
        [Key]
        public string Id { get; set; } = null!;

        [Required]
        public string AccommodationId { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public string? PhotoUrl { get; set; }

        [ForeignKey("AccommodationId")]
        public virtual Accommodation? Accommodation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
