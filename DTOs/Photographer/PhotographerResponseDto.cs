using System;
using System.Collections.Generic;
using NearU_Backend_Revised.DTOs.PhotographyPackage;

namespace NearU_Backend_Revised.DTOs.Photographer
{
    public class PhotographerResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public decimal BaseRatePerHour { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public string? OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<PhotographyPackageResponseDto> Packages { get; set; } = new();
    }
}
