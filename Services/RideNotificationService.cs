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

    // ── SignalR — state changes ───────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task NotifyStateChangeAsync(RideRequest rideRequest, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Ride {RideId} changed state to {Status} (student {StudentId}, rider {RiderId})",
            rideRequest.Id,
            rideRequest.Status,
            rideRequest.StudentId,
            rideRequest.RiderId);

        var payload = new
        {
            rideId                  = rideRequest.Id,
            status                  = rideRequest.Status.ToString(),
            updatedAtUtc            = rideRequest.UpdatedAt,
            otp                     = rideRequest.Status.ToString() == "Accepted" ? rideRequest.OTP : null,
            otpExpiresAt            = rideRequest.Status.ToString() == "Accepted" ? rideRequest.OtpExpiresAt : (DateTime?)null,
            riderId                 = rideRequest.RiderId,
            riderName               = rideRequest.Rider?.Username,
            riderPhoneNumber        = rideRequest.Rider?.MobileNumber,
            riderProfilePictureUrl  = rideRequest.Rider?.ProfilePictureUrl,
            studentId               = rideRequest.StudentId,
            studentName             = rideRequest.Student?.Username,
            studentPhoneNumber      = rideRequest.Student?.MobileNumber,
            studentProfilePictureUrl = rideRequest.Student?.ProfilePictureUrl
        };

        // 1. Broadcast to the ride channel (clients that already called JoinRideChannel)
        var rideTasks = new List<Task>
        {
            _hubContext.Clients.Group($"ride:{rideRequest.Id}")
                .SendAsync("RideStateChanged", payload, cancellationToken)
        };

        // 2. Push to the student's personal channel (always receives, no JoinRideChannel needed)
        if (!string.IsNullOrWhiteSpace(rideRequest.StudentId))
        {
            rideTasks.Add(
                _hubContext.Clients.Group($"user:{rideRequest.StudentId}")
                    .SendAsync("RideStateChanged", payload, cancellationToken));
        }

        // 3. Push to the rider's personal channel (when a rider is assigned)
        if (!string.IsNullOrWhiteSpace(rideRequest.RiderId))
        {
            rideTasks.Add(
                _hubContext.Clients.Group($"user:{rideRequest.RiderId}")
                    .SendAsync("RideStateChanged", payload, cancellationToken));
        }

        await Task.WhenAll(rideTasks);
    }

    // ── SignalR — live location ───────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task BroadcastLocationAsync(
        string rideId,
        string studentId,
        double latitude,
        double longitude,
        decimal? distanceToPickupKm,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            rideId,
            latitude,
            longitude,
            distanceToPickupKm,
            timestamp = DateTime.UtcNow
        };

        await Task.WhenAll(
            // Ride channel — both rider + student if they called JoinRideChannel
            _hubContext.Clients.Group($"ride:{rideId}")
                .SendAsync("LocationUpdated", payload, cancellationToken),
            // Student's personal channel — guaranteed delivery even without JoinRideChannel
            _hubContext.Clients.Group($"user:{studentId}")
                .SendAsync("LocationUpdated", payload, cancellationToken)
        );
    }

    // ── SignalR — new ride to online riders ──────────────────────────────────

    /// <inheritdoc/>
    public async Task NotifyNewRideToOnlineRidersAsync(RideRequest rideRequest, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Broadcasting NewRideAvailable for ride {RideId} to OnlineRiders group",
            rideRequest.Id);

        await _hubContext.Clients.Group("OnlineRiders")
            .SendAsync(
                "NewRideAvailable",
                new
                {
                    rideId      = rideRequest.Id,
                    serviceType = rideRequest.ServiceType.ToString(),
                    pickupLat   = rideRequest.PickupLocation?.Y,
                    pickupLng   = rideRequest.PickupLocation?.X,
                    dropoffLat  = rideRequest.DropoffLocation?.Y,
                    dropoffLng  = rideRequest.DropoffLocation?.X,
                    estimatedFare = rideRequest.EstimatedFare,
                    distanceKm    = rideRequest.CalculatedDistance,
                    createdAtUtc  = rideRequest.CreatedAt
                },
                cancellationToken);
    }

    // ── FCM — new ride request push to riders ─────────────────────────────────

    /// <inheritdoc/>
    public async Task SendNewRideRequestPushAsync(
        RideRequest rideRequest,
        IEnumerable<string> riderUserIds,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _fcmTokenService.GetTokensForUsersAsync(riderUserIds, cancellationToken);
        if (!tokens.Any())
        {
            _logger.LogDebug(
                "No FCM tokens found for nearby riders — skipping push for ride {RideId}",
                rideRequest.Id);
            return;
        }

        await SendMulticastPushAsync(
            tokens,
            title: "New Ride Nearby 🛵",
            body:  $"A new {rideRequest.ServiceType} request is available. Tap to view.",
            data: new Dictionary<string, string>
            {
                ["rideId"]      = rideRequest.Id,
                ["serviceType"] = rideRequest.ServiceType.ToString(),
                ["action"]      = "new_ride_request"
            },
            channelId: "ride_alerts",
            logContext: $"ride {rideRequest.Id}",
            cancellationToken: cancellationToken);
    }

    // ── FCM — status change push to student ───────────────────────────────────

    /// <inheritdoc/>
    public async Task SendRideStatusPushToStudentAsync(
        RideRequest rideRequest,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _fcmTokenService.GetTokensForUsersAsync(
            new[] { rideRequest.StudentId }, cancellationToken);

        if (!tokens.Any())
        {
            _logger.LogDebug(
                "No FCM tokens for student {StudentId} — skipping push for ride {RideId}",
                rideRequest.StudentId, rideRequest.Id);
            return;
        }

        await SendMulticastPushAsync(
            tokens,
            title: title,
            body:  body,
            data: new Dictionary<string, string>
            {
                ["rideId"] = rideRequest.Id,
                ["status"] = rideRequest.Status.ToString(),
                ["action"] = "ride_status_update"
            },
            channelId: "ride_updates",
            logContext: $"student {rideRequest.StudentId} / ride {rideRequest.Id}",
            cancellationToken: cancellationToken);
    }

    // ── FCM — status change push to rider ─────────────────────────────────────

    /// <inheritdoc/>
    public async Task SendRideStatusPushToRiderAsync(
        RideRequest rideRequest,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rideRequest.RiderId))
        {
            _logger.LogDebug(
                "No rider assigned to ride {RideId} — skipping rider push",
                rideRequest.Id);
            return;
        }

        var tokens = await _fcmTokenService.GetTokensForUsersAsync(
            new[] { rideRequest.RiderId }, cancellationToken);

        if (!tokens.Any())
        {
            _logger.LogDebug(
                "No FCM tokens for rider {RiderId} — skipping push for ride {RideId}",
                rideRequest.RiderId, rideRequest.Id);
            return;
        }

        await SendMulticastPushAsync(
            tokens,
            title: title,
            body:  body,
            data: new Dictionary<string, string>
            {
                ["rideId"] = rideRequest.Id,
                ["status"] = rideRequest.Status.ToString(),
                ["action"] = "ride_status_update"
            },
            channelId: "ride_updates",
            logContext: $"rider {rideRequest.RiderId} / ride {rideRequest.Id}",
            cancellationToken: cancellationToken);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private async Task SendMulticastPushAsync(
        IEnumerable<string> tokens,
        string title,
        string body,
        Dictionary<string, string> data,
        string channelId,
        string logContext,
        CancellationToken cancellationToken)
    {
        var tokenList = tokens.ToList();
        if (!tokenList.Any()) return;

        var message = new MulticastMessage
        {
            Tokens       = tokenList,
            Notification = new Notification { Title = title, Body = body },
            Data         = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = channelId,
                    Sound     = "default"
                }
            },
            Apns = new ApnsConfig
            {
                Aps = new Aps { Sound = "default", Badge = 1 }
            }
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance
                .SendEachForMulticastAsync(message, cancellationToken);

            _logger.LogInformation(
                "FCM push ({Context}): {Success}/{Total} delivered",
                logContext, response.SuccessCount, tokenList.Count);

            if (response.FailureCount > 0)
            {
                var failedTokens = response.Responses
                    .Select((r, i) => (r, tokenList[i]))
                    .Where(x => !x.r.IsSuccess)
                    .Select(x => x.Item2);

                _logger.LogWarning(
                    "Stale FCM tokens detected ({Context}): {Tokens}",
                    logContext, string.Join(", ", failedTokens));
            }
        }
        catch (Exception ex)
        {
            // Never let push notification failure crash the ride flow
            _logger.LogError(ex, "FCM multicast failed ({Context})", logContext);
        }
    }
}
