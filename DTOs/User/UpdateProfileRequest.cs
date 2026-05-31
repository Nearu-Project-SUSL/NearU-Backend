namespace NearU_Backend_Revised.DTOs.User
{
    public class UpdateProfileRequest
    {
        public string? Username { get; set; }
        public string? MobileNumber { get; set; }
        public string? Faculty { get; set; }
        public string? Year { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? DateOfBirth { get; set; }
    }
}
