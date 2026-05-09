using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.DTOs.Rides
{
    public class CreateRideRequestDto
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public string ServiceType { get; set; } = string.Empty; // PersonalRide, FoodDelivery, GroceryPickup

        public string? Details { get; set; }

        [Required]
        public double PickupLat { get; set; }

        [Required]
        public double PickupLon { get; set; }

        [Required]
        public double DropoffLat { get; set; }

        [Required]
        public double DropoffLon { get; set; }
    }
}