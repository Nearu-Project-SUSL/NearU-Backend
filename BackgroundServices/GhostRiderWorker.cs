using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;

namespace NearU_Backend_Revised.BackgroundServices
{
  public class GhostRiderWorker : BackgroundService
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GhostRiderWorker> _logger; // to log msg to console/log files

    public GhostRiderWorker(IServiceScopeFactory scopeFactory, ILogger<GhostRiderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while(!stoppingToken.IsCancellationRequested)  //run until app stop
      {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        await CleanupAsync();
      }
    }

    private async Task CleanupAsync()
    {
      using var scope = _scopeFactory.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      
      var cutoff = DateTime.UtcNow.AddMinutes(-5);

      var stale = await db.RideRequests
        .Where(r => r.Status == "Pending" && r.CreatedAt < cutoff)
        .ToListAsync();

      foreach (var ride in stale)
      {
        ride.Status = "Expired";
        ride.UpdatedAt = DateTime.UtcNow;
      }

      var count = await db.SaveChangesAsync();
      _logger.LogInformation("GhostRider: Expired {Count} ride requests", count);      
    }
  }
}