namespace NearU_Backend_Revised.DTOs.User
{
    public class UserProfileResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string? LastLoginDate { get; set; }
        public int IsActive { get; set; }
        
        public string? MobileNumber { get; set; }
        public string? StudentId { get; set; }
        public string? Faculty { get; set; }
        public string? Year { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
