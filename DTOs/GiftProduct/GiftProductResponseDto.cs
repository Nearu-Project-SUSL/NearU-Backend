using System;

namespace NearU_Backend_Revised.DTOs.GiftProduct
{
    public class GiftProductResponseDto
    {
        public Guid Id { get; set; }
        public Guid GiftShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}