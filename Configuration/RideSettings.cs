namespace NearU_Backend_Revised.Configuration
{
    public class RideSettings
    {
        public decimal BaseFare { get; set; }
        public decimal RatePerKm { get; set; }
        public int PendingTimeoutSeconds { get; set; } = 120;
        public int GhostRiderOfflineMinutes { get; set; } = 5;
        public int InterruptedAfterHeartbeatMinutes { get; set; } = 3;
        public int TrackingRetentionHours { get; set; } = 24;

        /// <summary>
        /// Maximum distance (in metres) from the faculty centroid within which
        /// both pickup and drop-off points must fall. Defaults to 20 km.
        /// Override via RideSettings__AllowedRadiusMeters env var or appsettings.
        /// </summary>
        public int AllowedRadiusMeters { get; set; } = 20000;
    }
}
