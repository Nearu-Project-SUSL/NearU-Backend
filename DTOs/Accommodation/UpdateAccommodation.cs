using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Accommodation
{
    public class UpdateAccommodation
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public IFormFile? Photo { get; set; }
        public string? Type { get; set; }
        public decimal? DistanceKm { get; set; }
        public decimal? MonthlyRent { get; set; }
        public int? AvailableBeds { get; set; }
        public string? Amenities { get; set; }
    }
}
