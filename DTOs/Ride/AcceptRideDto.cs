using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Rides
{
    public class AcceptRideDto
    {
        [Required]
        public Guid RideId { get; set; }

        [Required]
        public Guid RiderId { get; set; }
    }
}