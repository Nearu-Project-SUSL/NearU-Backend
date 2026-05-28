using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Auth
{
    public class VerifyResetCodeRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }
}
