namespace NearU_Backend_Revised.DTOs.Rides
{
    public class CreateRideRequestDto
    {
        public Guid UserId { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public double PickupLat { get; set; }
        public double PickupLon { get; set; }
        public double DropoffLat { get; set; }
        public double DropoffLon { get; set; }
    }
}