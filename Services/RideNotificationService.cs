using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using NearU_Backend_Revised.Hubs;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Services;

public class RideNotificationService : IRideNotificationService
{
    private readonly ILogger<RideNotificationService> _logger;
    private readonly IHubContext<RidesHub> _hubContext;
    private readonly IFcmTokenService _fcmTokenService;

    public RideNotificationService(
        ILogger<RideNotificationService> logger,
        IHubContext<RidesHub> hubContext,
        IFcmTokenService fcmTokenService)
    {
        _logger = logger;
        _hubContext = hubContext;
        _fcmTokenService = fcmTokenService;
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

    public async Task BroadcastLocationAsync(string rideId, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"ride:{rideId}")
            .SendAsync(
                "LocationUpdated",
                new
                {
                    rideId,
                    latitude,
                    longitude,
                    timestamp = DateTime.UtcNow
                },
                cancellationToken);
    }

    public async Task SendNewRideRequestPushAsync(
        RideRequest rideRequest,
        IEnumerable<string> riderUserIds,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _fcmTokenService.GetTokensForUsersAsync(riderUserIds, cancellationToken);
        if (!tokens.Any())
        {
            _logger.LogDebug("No FCM tokens found for nearby riders — skipping push for ride {RideId}", rideRequest.Id);
            return;
        }

        var message = new MulticastMessage
        {
            Tokens = tokens.ToList(),
            Notification = new Notification
            {
                Title = "New Ride Nearby 🛵",
                Body = $"A new {rideRequest.ServiceType} request is available. Tap to view."
            },
            Data = new Dictionary<string, string>
            {
                ["rideId"]      = rideRequest.Id,
                ["serviceType"] = rideRequest.ServiceType.ToString(),
                ["action"]      = "new_ride_request"
            },
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = "ride_alerts",
                    Sound     = "default"
                }
            },
            Apns = new ApnsConfig
            {
                Aps = new Aps
                {
                    Sound = "default",
                    Badge = 1
                }
            }
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);
            _logger.LogInformation(
                "FCM push for ride {RideId}: {Success} success, {Failure} failure out of {Total} tokens",
                rideRequest.Id, response.SuccessCount, response.FailureCount, tokens.Count());

            // Log failed tokens for cleanup (stale tokens should be removed)
            if (response.FailureCount > 0)
            {
                var failedTokens = response.Responses
                    .Select((r, i) => (r, tokens.ElementAt(i)))
                    .Where(x => !x.r.IsSuccess)
                    .Select(x => x.Item2);

                _logger.LogWarning("Stale FCM tokens detected for ride {RideId}: {Tokens}", rideRequest.Id, string.Join(", ", failedTokens));
            }
        }
        catch (Exception ex)
        {
            // Never let push notification failure crash the ride flow
            _logger.LogError(ex, "FCM multicast failed for ride {RideId}", rideRequest.Id);
        }
    }
}
