using NearU_Backend_Revised.Enums;

namespace NearU_Backend_Revised.DTOs.Ride;

/// <summary>
/// Summary entry returned by GET /api/rides/history
/// </summary>
public class RideHistoryDto
{
    public string HistoryId { get; set; } = string.Empty;
    public string RideId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string? RiderId { get; set; }
    public RideServiceType ServiceType { get; set; }
    public decimal FinalFare { get; set; }
    public decimal DistanceKm { get; set; }
    public DateTime CompletedAt { get; set; }
    public int? RiderRating { get; set; }
    public int? StudentRating { get; set; }
}

/// <summary>
/// Request body for POST /api/rides/history/{rideId}/rate
/// </summary>
public class RateRideRequestDto
{
    /// <summary>Rating from 1 to 5.</summary>
    public int Rating { get; set; }
}

/// <summary>
/// Response from GET /api/rides/estimate
/// </summary>
public class FareEstimateResponseDto
{
    public decimal EstimatedFare { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal BaseFare { get; set; }
    public decimal RatePerKm { get; set; }
}
