using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.Models
{
    public class GiftShop
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [MaxLength(150)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<GiftProduct> Products { get; set; } = new List<GiftProduct>();
    }
}