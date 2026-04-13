namespace NearU_Backend_Revised.DTOs.Accommodation
{
    public class AccommodationResponse
    {
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? PhotoUrl { get; set; }
    }
}
