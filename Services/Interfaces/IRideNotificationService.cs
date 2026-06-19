using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Services.Interfaces;

public interface IRideNotificationService
{
    /// <summary>
    /// Broadcasts a real-time state change to:
    ///   - ride:{rideId} group (clients subscribed via JoinRideChannel)
    ///   - user:{studentId} personal channel
    ///   - user:{riderId} personal channel (when a rider is assigned)
    /// This ensures the student/rider receives the event even before calling JoinRideChannel.
    /// </summary>
    Task NotifyStateChangeAsync(RideRequest rideRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts the rider's GPS coordinates and distance to pickup to the student tracking the ride.
    /// Sends to both ride:{rideId} and user:{studentId} groups.
    /// </summary>
    Task BroadcastLocationAsync(string rideId, string studentId, double latitude, double longitude, decimal? distanceToPickupKm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a SignalR 'NewRideAvailable' event to all riders in the 'OnlineRiders' group.
    /// Used when a student creates a new ride request so online riders see it instantly.
    /// </summary>
    Task NotifyNewRideToOnlineRidersAsync(RideRequest rideRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an FCM push notification to all online riders to notify them of a new ride request nearby.
    /// Only sends if the rider has a stored FCM device token (app backgrounded/closed).
    /// </summary>
    Task SendNewRideRequestPushAsync(RideRequest rideRequest, IEnumerable<string> riderUserIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an FCM push notification to the student to notify them of a ride status change
    /// (e.g. rider accepted, rider arrived, ride completed).
    /// </summary>
    Task SendRideStatusPushToStudentAsync(RideRequest rideRequest, string title, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an FCM push notification to the rider to notify them of a student action
    /// (e.g. student cancelled after acceptance).
    /// Only fires if a rider is assigned to the ride.
    /// </summary>
    Task SendRideStatusPushToRiderAsync(RideRequest rideRequest, string title, string body, CancellationToken cancellationToken = default);
}
