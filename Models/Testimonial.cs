using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace NearU_Backend_Revised.Models
{
    public class Testimonial
    {
        [Key]
        public int Id { get; set; }

        // string — matches User.Id type
        public string UserId { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}