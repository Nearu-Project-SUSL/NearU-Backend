using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Accommodation
{
	public class CreateAccommodation
	{
		[Required(ErrorMessage = "Accommodation name is required")]
		public string Name { get; set; } = null!;

		public string? Description { get; set; }

		public string? Address { get; set; }

		public string? PhoneNumber { get; set; }

		public IFormFile? Photo { get; set; }

		// Type: Boarding, Annex, Apartment
		public string? Type { get; set; }

		public decimal DistanceKm { get; set; } = 0;

		public decimal MonthlyRent { get; set; } = 0;

		public int AvailableBeds { get; set; } = 0;

		// Comma-separated amenities e.g. "Wi-Fi,Laundry,Meals"
		public string? Amenities { get; set; }
    }
}
