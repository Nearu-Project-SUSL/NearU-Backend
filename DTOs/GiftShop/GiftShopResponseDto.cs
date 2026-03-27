using System;
using System.Collections.Generic;
using NearU_Backend_Revised.DTOs.GiftProduct;

namespace NearU_Backend_Revised.DTOs.GiftShop
{
    public class GiftShopResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<GiftProductResponseDto> Products { get; set; } = new();
    }
}