using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Rides
{
    public class VerifyOtpDto
    {
        [Required]
        public Guid RideId { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 4)]
        public string EnteredOtp { get; set; } = string.Empty;
    }
}