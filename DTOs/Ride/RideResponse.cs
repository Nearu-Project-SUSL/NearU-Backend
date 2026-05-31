namespace NearU_Backend_Revised.DTOs.Rides
{
    public class RideResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid? RiderId { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? OTP { get; set; }   // only shown after acceptance
        public decimal? Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}