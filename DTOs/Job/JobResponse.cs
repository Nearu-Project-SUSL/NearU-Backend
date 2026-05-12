namespace NearU_Backend_Revised.DTOs.Job
{
    public class JobResponse
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Company { get; set; } = null!;
        public string Location { get; set; } = null!;
        public string PayRange { get; set; } = null!;
        public string JobType { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string? Logo { get; set; }
        public string Description { get; set; } = null!;
        public string? LongDescription { get; set; }
        public List<string>? Requirements { get; set; }
        public List<string>? Tags { get; set; }
        public bool IsNew { get; set; }
        public PostedByInfo PostedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string PostedAt { get; set; } = null!;
    }

    public class PostedByInfo
    {
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Avatar { get; set; }
        public string? MobileNumber { get; set; }
    }
}