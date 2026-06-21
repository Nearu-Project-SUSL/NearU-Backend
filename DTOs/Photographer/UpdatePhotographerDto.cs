using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Photographer
{
    public class UpdatePhotographerDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Bio { get; set; }

        [Required]
        [Range(0, 9999999.99)]
        public decimal BaseRatePerHour { get; set; }

        [Required]
        [MaxLength(150)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        public bool IsActive { get; set; }

        public IFormFile? Image { get; set; }
    }
}
