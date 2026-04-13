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
    }
}
