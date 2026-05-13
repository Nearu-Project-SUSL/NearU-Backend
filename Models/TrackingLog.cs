using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace NearU_Backend_Revised.Models;

public class TrackingLog
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string RideId { get; set; } = null!;

    [ForeignKey("RideId")]
    public virtual RideRequest RideRequest { get; set; } = null!;

    [Column(TypeName = "geography(Point, 4326)")]
    public Point Coordinates { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
