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
    }
}
