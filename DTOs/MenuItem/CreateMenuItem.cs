using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.MenuItem
{
    public class CreateMenuItem
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public decimal Price { get; set; } = decimal.Zero;

        public string? Description { get; set; }

        public IFormFile? Photo { get; set; }
    }
}