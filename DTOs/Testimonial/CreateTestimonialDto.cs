using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs
{
    public class CreateTestimonialDto
    {
        [Required]
        [MinLength(10, ErrorMessage = "Message must be at least 10 characters")]
        [MaxLength(500, ErrorMessage = "Message cannot exceed 500 characters")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }
    }
}