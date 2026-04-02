using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.GiftProduct
{
    public class CreateGiftProductDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public IFormFile? Photo { get; set; }

        [Range(0, 9999999.99)]
        public decimal Price { get; set; }
    }
}