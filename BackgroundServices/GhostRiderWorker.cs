using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;

namespace NearU_Backend_Revised.BackgroundServices
{
  public class GhostRiderWorker : BackgroundService
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GhostRiderWorker> _logger;

    public GhostRiderWorker(IServiceScopeFactory scopeFactory, ILogger<GhostRiderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while(!stoppingToken.IsCancellationRequested)
      {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        await CleanupAsync();
      }
    }
  }
}