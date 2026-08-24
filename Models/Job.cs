using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NearU_Backend_Revised.Models
{     public class Job
    {
        [Key]
        public string Id { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Company { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string PayRange { get; set; } = string.Empty;

        //Part-Time, Full-Time, Internship, Freelance, campus, etc.
        [Required]
        [MaxLength(50)]
        public string JobType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;
        public string? Logo { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? LongDescription { get; set; }

        public string? Requirements { get; set; }
        public string? Tags { get; set; }

        public bool IsNew { get; set; } = true;


        [Required]
        [MaxLength(100)]
        public string PostedByName { get; set; } = string.Empty;

        [Required]
        public string PostedByUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("PostedByUserId")]
        public virtual User? PostedByUser { get; set; }
    }
}