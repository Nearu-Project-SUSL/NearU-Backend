using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using NearU_Backend_Revised.Enums;

namespace NearU_Backend_Revised.Models;

public class RiderStatus
{
    [Key]
    public string RiderId { get; set; } = null!;

    [ForeignKey("RiderId")]
    public virtual User User { get; set; } = null!;

    public bool IsOnline { get; set; }

    [Column(TypeName = "geography(Point, 4326)")]
    public Point? LastLocation { get; set; }

    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public RiderApprovalStatus ApprovalStatus { get; set; } = RiderApprovalStatus.Pending;
    
    public RiderTier RiderTier { get; set; } = RiderTier.Standard;
}
