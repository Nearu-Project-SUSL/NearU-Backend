using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NearU_Backend_Revised.Models
{
    public class Deal
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(100)]
        public string ShopName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string ShopType { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string BadgeText { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string BadgeColor { get; set; } = "#ef4444";

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        [Required]
        public string SubmittedByUserId { get; set; } = null!;

        [ForeignKey("SubmittedByUserId")]
        public virtual User SubmittedByUser { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
