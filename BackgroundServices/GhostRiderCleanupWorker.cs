using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Data;

namespace NearU_Backend_Revised.BackgroundServices;

public class GhostRiderCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RideSettings _rideSettings;
    private readonly ILogger<GhostRiderCleanupWorker> _logger;

    public GhostRiderCleanupWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RideSettings> rideSettings,
        ILogger<GhostRiderCleanupWorker> logger)
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
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var staleThreshold = DateTime.UtcNow.AddMinutes(-_rideSettings.GhostRiderOfflineMinutes);
                var staleOnlineRiders = await db.RiderStatuses
                    .Where(rs => rs.IsOnline && rs.LastSeen <= staleThreshold)
                    .ToListAsync(stoppingToken);

                if (staleOnlineRiders.Count > 0)
                {
                    foreach (var rider in staleOnlineRiders)
                    {
                        rider.IsOnline = false;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ghost rider cleanup cycle failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
