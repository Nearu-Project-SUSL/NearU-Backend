namespace NearU_Backend_Revised.DTOs.Cache
{
    /// <summary>
    /// Slim, serialiser-friendly view of a rider's real-time status stored in Redis.
    /// Avoids pulling NetTopologySuite.Geometries.Point into the JSON context.
    /// </summary>
    public sealed class CachedRiderStatus
    {
        public string RiderId { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        /// <summary>WGS-84 longitude, or null when no location is known.</summary>
        public double? Longitude { get; set; }
        /// <summary>WGS-84 latitude, or null when no location is known.</summary>
        public double? Latitude { get; set; }
        public DateTime LastSeen { get; set; }
        /// <summary>String representation of <see cref="Enums.RiderApprovalStatus"/>.</summary>
        public string ApprovalStatus { get; set; } = "Pending";
    }
}
