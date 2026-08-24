using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace NearU_Backend_Revised.Hubs;

/// <summary>
/// Real-time hub for the NearU Rides feature.
/// Requires a valid JWT — pass it as the `access_token` query string when connecting
/// (e.g. wss://api.nearusab.me/hubs/rides?access_token=eyJ...).
///
/// Group naming convention:
///   ride:{rideId}   — all participants of a specific ride
///   user:{userId}   — personal channel; server pushes to this on every state change
///                     so clients receive updates even before calling JoinRideChannel
///   OnlineRiders    — all riders who have called GoOnline()
///   Admins          — admin users
/// </summary>
[Authorize]
public class RidesHub : Hub
{
    private readonly ILogger<RidesHub> _logger;

    public RidesHub(ILogger<RidesHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue("userId");
        var role   = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        // Auto-join personal channel — no extra call needed from the client.
        // The server uses user:{userId} to push ride state changes directly to
        // this user regardless of which ride they are in.
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            _logger.LogInformation(
                "User {UserId} (role: {Role}) connected — auto-joined user:{UserId} channel",
                userId, role, userId);
        }

        if (role == "Admin")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            _logger.LogInformation(
                "Admin connection established: {ConnectionId} for user {UserId}",
                Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    // ── Ride channel ─────────────────────────────────────────────────────────

    /// <summary>
    /// Subscribe to updates for a specific ride.
    /// Both the student and the matched rider should call this after a ride is accepted.
    /// Note: state changes are also pushed to the personal user:{userId} group, so this
    /// call is not strictly required but is recommended for location tracking events.
    /// </summary>
    public Task JoinRideChannel(string rideId)
    {
        var userId = Context.User?.FindFirstValue("userId") ?? Context.ConnectionId;
        _logger.LogInformation("User {UserId} joined ride channel ride:{RideId}", userId, rideId);
        return Groups.AddToGroupAsync(Context.ConnectionId, $"ride:{rideId}");
    }

    /// <summary>
    /// Unsubscribe from a ride channel (e.g. after the ride completes or is cancelled).
    /// </summary>
    public Task LeaveRideChannel(string rideId)
    {
        var userId = Context.User?.FindFirstValue("userId") ?? Context.ConnectionId;
        _logger.LogInformation("User {UserId} left ride channel ride:{RideId}", userId, rideId);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride:{rideId}");
    }

    // ── Rider availability ────────────────────────────────────────────────────

    /// <summary>
    /// Called by riders when they go online — adds them to the 'OnlineRiders' group
    /// so the server can broadcast new nearby ride requests to all available riders.
    /// </summary>
    public Task GoOnline()
    {
        var userId = Context.User?.FindFirstValue("userId") ?? Context.ConnectionId;
        _logger.LogInformation("Rider {UserId} went online via SignalR", userId);
        return Groups.AddToGroupAsync(Context.ConnectionId, "OnlineRiders");
    }

    /// <summary>
    /// Called by riders when they go offline or close the app.
    /// </summary>
    public Task GoOffline()
    {
        var userId = Context.User?.FindFirstValue("userId") ?? Context.ConnectionId;
        _logger.LogInformation("Rider {UserId} went offline via SignalR", userId);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, "OnlineRiders");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Auto-clean on unexpected disconnects
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "OnlineRiders");
        await base.OnDisconnectedAsync(exception);
    }
}
