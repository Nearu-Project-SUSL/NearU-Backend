using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.PhotographyPackage
{
    public class UpdatePhotographyPackageDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, 9999999.99)]
        public decimal Price { get; set; }

        [MaxLength(300)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
