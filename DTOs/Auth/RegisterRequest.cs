using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// The role the user is registering as.
        /// Allowed values: Student, Rider, Business.
        /// Admin accounts cannot be created through this endpoint.
        /// </summary>
        [Required]
        [RegularExpression(
            "^(Student|Rider|Business)$",
            ErrorMessage = "Role must be one of: Student, Rider, Business. Admin accounts cannot be self-registered."
        )]
        public string Role { get; set; } = string.Empty;

        public string? MobileNumber { get; set; }
        public string? StudentId { get; set; }
        public string? Faculty { get; set; }
        public string? Year { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? DateOfBirth { get; set; }
        public string? BusinessType  { get; set; }
        public string? BusinessName  { get; set; }
        public string? OwnerName     { get; set; }
        public string? Description   { get; set; }
    }
}
