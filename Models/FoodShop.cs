using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace NearU_Backend_Revised.Models
{
    public class FoodShop
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

        public string Category { get; set; } = "Other";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The Business user who owns/manages this shop.
        /// Nullable to allow Admin-created shops without a specific owner.
        /// </summary>
        public string? OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public virtual User? Owner { get; set; }

        // Navigation property for menu items
        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
