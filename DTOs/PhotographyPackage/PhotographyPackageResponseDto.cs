using System;

namespace NearU_Backend_Revised.DTOs.PhotographyPackage
{
    public class PhotographyPackageResponseDto
    {
        public Guid Id { get; set; }
        public Guid PhotographerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
