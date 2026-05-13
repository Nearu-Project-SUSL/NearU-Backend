using System.ComponentModel.DataAnnotations;
using NearU_Backend_Revised.Enums;

namespace NearU_Backend_Revised.Models;

public class RideHistory
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string RideId { get; set; } = null!;

    public string StudentId { get; set; } = null!;

    public string? RiderId { get; set; }

    public RideServiceType ServiceType { get; set; }

    public decimal FinalFare { get; set; }

    public decimal CalculatedDistance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
