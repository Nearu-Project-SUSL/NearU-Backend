using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.AccommodationItem
{
    public class UpdateAccommodationItem
    {

        public string? Name { get; set; }
        
        public decimal? Price { get; set; }

        public string? Description { get; set; }

        public IFormFile? Photo { get; set; }
    }
}
