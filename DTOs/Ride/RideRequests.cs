using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using NearU_Backend_Revised.Enums;

namespace NearU_Backend_Revised.DTOs.Ride;

public class CreateRideRequestDto
{
    [Required]
    public RideServiceType ServiceType { get; set; }

    [Required]
    public JsonElement Details { get; set; }

    [Range(-90, 90)]
    public double PickupLatitude { get; set; }

    [Range(-180, 180)]
    public double PickupLongitude { get; set; }

    [Range(-90, 90)]
    public double DropoffLatitude { get; set; }

    [Range(-180, 180)]
    public double DropoffLongitude { get; set; }

    public bool ConfirmEstimate { get; set; }
}

public class RideIdRequestDto
{
    [Required]
    public string RideId { get; set; } = string.Empty;
}

public class VerifyOtpRequestDto : RideIdRequestDto
{
    [Required]
    [StringLength(4, MinimumLength = 4)]
    public string Otp { get; set; } = string.Empty;
}

public class RiderStatusUpdateRequestDto
{
    public bool IsOnline { get; set; }
}

public class LocationHeartbeatRequestDto : RideIdRequestDto
{
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }
}

public class RideSummaryDto
{
    public string RideId { get; set; } = string.Empty;
    public RideRequestStatus Status { get; set; }
    public RideServiceType ServiceType { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string? RiderId { get; set; }
    public decimal EstimatedFare { get; set; }
    public decimal DistanceKm { get; set; }
    // Pickup coordinates — used by client to render map pins
    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }
    // Dropoff coordinates
    public double DropoffLatitude { get; set; }
    public double DropoffLongitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
}

public class RideLocationResponseDto
{
    public string RideId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
