using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Job
{
    public class UpdateJob
    {
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
        public string? Title { get; set; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 100 characters")]
        public string? Company { get; set; }

        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
        public string? Location { get; set; }

        [StringLength(100, ErrorMessage = "Pay range cannot exceed 100 characters")]
        public string? PayRange { get; set; }

        [StringLength(50, ErrorMessage = "Job type cannot exceed 50 characters")]
        public string? JobType { get; set; }

        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string? Category { get; set; }

        [Url(ErrorMessage = "Logo must be a valid URL")]
        public string? Logo { get; set; }

        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 500 characters")]
        public string? Description { get; set; }

        [StringLength(2000, ErrorMessage = "Long description cannot exceed 2000 characters")]
        public string? LongDescription { get; set; }

        public List<string>? Requirements { get; set; }
        public List<string>? Tags { get; set; }
        public bool? IsNew { get; set; }
    }
}