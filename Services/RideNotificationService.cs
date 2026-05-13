using Microsoft.AspNetCore.SignalR;
using NearU_Backend_Revised.Hubs;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services;

public class RideNotificationService : IRideNotificationService
{
    private readonly ILogger<RideNotificationService> _logger;
    private readonly IHubContext<RidesHub> _hubContext;

    public RideNotificationService(ILogger<RideNotificationService> logger, IHubContext<RidesHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task NotifyStateChangeAsync(RideRequest rideRequest, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Ride {RideId} changed state to {Status} (student {StudentId}, rider {RiderId})",
            rideRequest.Id,
            rideRequest.Status,
            rideRequest.StudentId,
            rideRequest.RiderId);

        await _hubContext.Clients.Group($"ride:{rideRequest.Id}")
            .SendAsync(
                "RideStateChanged",
                new
                {
                    rideId = rideRequest.Id,
                    status = rideRequest.Status.ToString(),
                    updatedAtUtc = rideRequest.UpdatedAt
                },
                cancellationToken);
    }
}
