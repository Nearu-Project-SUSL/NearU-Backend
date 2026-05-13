using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Enums;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.BackgroundServices;

public class RideLifecycleWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RideSettings _rideSettings;
    private readonly ILogger<RideLifecycleWorker> _logger;

    public RideLifecycleWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RideSettings> rideSettings,
        ILogger<RideLifecycleWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _rideSettings = rideSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ride lifecycle cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<IRideNotificationService>();
        var now = DateTime.UtcNow;

        var expiringBefore = now.AddSeconds(-_rideSettings.PendingTimeoutSeconds);
        var expiredRides = await db.RideRequests
            .Where(r => r.Status == RideRequestStatus.Pending && r.CreatedAt <= expiringBefore)
            .ToListAsync(cancellationToken);

        foreach (var ride in expiredRides)
        {
            ride.Status = RideRequestStatus.Expired;
            ride.CancelledAt = now;
            ride.UpdatedAt = now;
        }

        var interruptedBefore = now.AddMinutes(-_rideSettings.InterruptedAfterHeartbeatMinutes);
        var interruptedRides = await db.RideRequests
            .Where(r => r.Status == RideRequestStatus.InProgress &&
                        r.LastHeartbeatAt.HasValue &&
                        r.LastHeartbeatAt.Value <= interruptedBefore)
            .ToListAsync(cancellationToken);

        foreach (var ride in interruptedRides)
        {
            ride.Status = RideRequestStatus.Interrupted;
            ride.UpdatedAt = now;
        }

        var retentionThreshold = now.AddHours(-_rideSettings.TrackingRetentionHours);
        var logsToPurge = await db.TrackingLogs
            .Where(t => t.Timestamp <= retentionThreshold &&
                        db.RideRequests.Any(r =>
                            r.Id == t.RideId &&
                            (r.Status == RideRequestStatus.Completed || r.Status == RideRequestStatus.Cancelled)))
            .ToListAsync(cancellationToken);

        if (logsToPurge.Count > 0)
        {
            db.TrackingLogs.RemoveRange(logsToPurge);
        }

        if (expiredRides.Count > 0 || interruptedRides.Count > 0 || logsToPurge.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var ride in expiredRides.Concat(interruptedRides))
        {
            await notifier.NotifyStateChangeAsync(ride, cancellationToken);
        }
    }
}
