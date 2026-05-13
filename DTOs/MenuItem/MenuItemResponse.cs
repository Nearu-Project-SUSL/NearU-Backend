namespace NearU_Backend_Revised.DTOs.MenuItem
{
    public class MenuItemResponse
    {
        public string Id { get; set; } = null!;

        public string FoodShopId { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string? PhotoUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}

