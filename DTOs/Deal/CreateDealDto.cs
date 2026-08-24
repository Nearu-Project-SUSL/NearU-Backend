using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NearU_Backend_Revised.DTOs.Deal
{
    public class CreateDealDto
    {
        [Required(ErrorMessage = "Shop name is required")]
        [StringLength(100, ErrorMessage = "Shop name cannot exceed 100 characters")]
        public string ShopName { get; set; } = null!;

        [Required(ErrorMessage = "Shop type is required")]
        [StringLength(50, ErrorMessage = "Shop type cannot exceed 50 characters")]
        public string ShopType { get; set; } = null!;

        [Required(ErrorMessage = "Title is required")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Badge text is required")]
        [StringLength(50, ErrorMessage = "Badge text cannot exceed 50 characters")]
        public string BadgeText { get; set; } = null!;

        [StringLength(20, ErrorMessage = "Badge color cannot exceed 20 characters")]
        public string BadgeColor { get; set; } = "#ef4444";

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        public IFormFile? Image { get; set; }
    }
}
