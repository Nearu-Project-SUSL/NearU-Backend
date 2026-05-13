using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Services.Interfaces;

public interface IRideNotificationService
{
    Task NotifyStateChangeAsync(RideRequest rideRequest, CancellationToken cancellationToken = default);
    Task BroadcastLocationAsync(string rideId, double latitude, double longitude, CancellationToken cancellationToken = default);
}
