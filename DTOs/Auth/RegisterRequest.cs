namespace NearU_Backend_Revised.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
        public string? StudentId { get; set; }
        public string? Faculty { get; set; }
        public string? Year { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? DateOfBirth { get; set; }
    }
}
