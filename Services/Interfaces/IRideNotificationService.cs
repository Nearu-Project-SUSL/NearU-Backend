using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Services.Interfaces;

public interface IRideNotificationService
{
    /// <summary>Broadcasts a real-time state change to all SignalR clients subscribed to the ride group.</summary>
    Task NotifyStateChangeAsync(RideRequest rideRequest, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts the rider's GPS coordinates to the student tracking the ride.</summary>
    Task BroadcastLocationAsync(string rideId, double latitude, double longitude, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an FCM push notification to all online riders to notify them of a new ride request nearby.
    /// Only sends if the rider has a stored FCM device token.
    /// </summary>
    Task SendNewRideRequestPushAsync(RideRequest rideRequest, IEnumerable<string> riderUserIds, CancellationToken cancellationToken = default);
}
