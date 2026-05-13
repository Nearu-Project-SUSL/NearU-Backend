using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using NearU_Backend_Revised.Enums;

namespace NearU_Backend_Revised.Models;

public class RideRequest
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string StudentId { get; set; } = null!;

    [ForeignKey("StudentId")]
    public virtual User Student { get; set; } = null!;

    public string? RiderId { get; set; }

    [ForeignKey("RiderId")]
    public virtual User? Rider { get; set; }

    public RideServiceType ServiceType { get; set; } = RideServiceType.PersonalRide;

    [Column(TypeName = "jsonb")]
    public string Details { get; set; } = "{}";

    public RideRequestStatus Status { get; set; } = RideRequestStatus.Pending;

    [Column(TypeName = "geography(Point, 4326)")]
    public Point PickupLocation { get; set; } = null!;

    [Column(TypeName = "geography(Point, 4326)")]
    public Point DropoffLocation { get; set; } = null!;

    public string? OTP { get; set; }

    public DateTime? OtpExpiresAt { get; set; }

    public int OTPAttempts { get; set; }

    public decimal EstimatedFare { get; set; }

    public decimal CalculatedDistance { get; set; }

    [Column(TypeName = "jsonb")]
    public string PriceRateSnapshot { get; set; } = "{}";

    public bool PenaltyApplied { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AcceptedAt { get; set; }

    public DateTime? ArrivedAt { get; set; }

    public DateTime? InProgressAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? LastHeartbeatAt { get; set; }

    [InverseProperty("RideRequest")]
    public virtual ICollection<TrackingLog> TrackingLogs { get; set; } = new List<TrackingLog>();
}
