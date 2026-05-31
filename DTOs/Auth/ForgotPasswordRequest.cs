using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Auth
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
