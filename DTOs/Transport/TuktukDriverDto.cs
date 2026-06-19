namespace NearU.Api.DTOs.Transport
{
    // Returned to clients
    public class TukTukDriverDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string? OperatingArea { get; set; }
        public string? Notes { get; set; }
    }

    // Used for both create and update (admin only)
    public class TukTukDriverUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string? OperatingArea { get; set; }
        public string? Notes { get; set; }
    }
}