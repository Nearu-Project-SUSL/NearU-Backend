using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.GiftShop
{
    public class UpdateGiftShopDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }

        [Required]
        [MaxLength(150)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}