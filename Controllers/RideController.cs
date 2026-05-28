using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs.Ride;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;
using System.Security.Claims;

namespace NearU_Backend_Revised.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class RideController : ControllerBase
{
    private readonly IRideService _rideService;
    private readonly IFcmTokenService _fcmTokenService;

    public RideController(IRideService rideService, IFcmTokenService fcmTokenService)
    {
        _rideService = rideService;
        _fcmTokenService = fcmTokenService;
    }

    // ─── FCM Device Token ────────────────────────────────────────────────────────

    /// <summary>POST /api/rides/device-token — call after login to enable push notifications.</summary>
    [HttpPost("rides/device-token")]
    [Authorize] // Available to all authenticated roles
    public async Task<IActionResult> RegisterDeviceToken([FromBody] FcmTokenRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = RequireUserId();
            await _fcmTokenService.UpsertTokenAsync(userId, request.Token, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse("Device registered for push notifications.", null));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    /// <summary>DELETE /api/rides/device-token — call on logout to remove push notifications.</summary>
    [HttpDelete("rides/device-token")]
    [Authorize] // Available to all authenticated roles
    public async Task<IActionResult> RemoveDeviceToken([FromBody] FcmTokenRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = RequireUserId();
            await _fcmTokenService.RemoveTokenAsync(userId, request.Token, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse("Device token removed.", null));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    // ─── Fare Estimate ──────────────────────────────────────────────────────────

    /// <summary>GET /api/rides/estimate — no ride is created, pure fare calculation.</summary>
    [HttpGet("rides/estimate")]
    [Authorize] // Available to all authenticated roles
    public async Task<IActionResult> GetEstimate(
        [FromQuery] double pickupLat, [FromQuery] double pickupLng,
        [FromQuery] double dropoffLat, [FromQuery] double dropoffLng,
        CancellationToken cancellationToken)
    {
        try
        {
            var estimate = await _rideService.EstimateFareAsync(pickupLat, pickupLng, dropoffLat, dropoffLng, cancellationToken);
            return Ok(ApiResponse<FareEstimateResponseDto>.SuccessResponse("Fare estimate calculated.", estimate));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    // ─── Ride History ────────────────────────────────────────────────────────────

    /// <summary>GET /api/rides/history — paginated history for the logged-in user (student or rider).</summary>
    [HttpGet("rides/history")]
    [Authorize] // Available to all authenticated roles
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = RequireUserId();
            var history = await _rideService.GetHistoryAsync(userId, page, pageSize, cancellationToken);
            return Ok(ApiResponse<IEnumerable<RideHistoryDto>>.SuccessResponse("History fetched.", history));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    /// <summary>POST /api/rides/history/{rideId}/rate — rate a completed ride (1–5 stars).</summary>
    [HttpPost("rides/history/{rideId}/rate")]
    [Authorize(Policy = "RequireStudent")] // Only students can rate rides
    public async Task<IActionResult> RateRide(string rideId, [FromBody] RateRideRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = RequireUserId();
            await _rideService.RateRideAsync(userId, rideId, request.Rating, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse("Rating submitted. Thank you!", new { rideId, request.Rating }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    // ─── Active Ride ──────────────────────────────────────────────────────────────

    /// <summary>GET /api/rides/active — returns current in-progress ride or 204 if none.</summary>
    [HttpGet("rides/active")]
    [Authorize] // Available to all authenticated roles
    public async Task<IActionResult> GetActiveRide(CancellationToken cancellationToken)
    {
        try
        {
            var userId = RequireUserId();
            var ride = await _rideService.GetActiveRideAsync(userId, cancellationToken);
            if (ride is null)
                return NoContent(); // 204 — no active ride, client shows "Find a ride" screen
            return Ok(ApiResponse<RideSummaryDto>.SuccessResponse("Active ride found.", ride));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    // ─── Ride Requests ───────────────────────────────────────────────────────────

    [HttpPost("requests")]
    [Authorize(Policy = "RequireStudent")] // Only students can request rides
    public async Task<IActionResult> CreateRequest([FromBody] CreateRideRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var studentId = RequireUserId();
            var result = await _rideService.CreateRequestAsync(studentId, request, cancellationToken);
            return Created($"/api/requests/{result.RideId}", ApiResponse<RideSummaryDto>.SuccessResponse("Request submitted.", result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpGet("requests/nearby")]
    [Authorize(Policy = "RequireRider")] // Only riders can see nearby requests
    public async Task<IActionResult> GetNearbyRequests([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusMeters = 5000, CancellationToken cancellationToken = default)
    {
        try
        {
            var riderId = RequireUserId();
            var result = await _rideService.GetNearbyRequestsAsync(riderId, latitude, longitude, radiusMeters, cancellationToken);
            return Ok(ApiResponse<IEnumerable<RideSummaryDto>>.SuccessResponse("Nearby requests fetched.", result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpPost("accept")]
    [Authorize(Policy = "RequireRider")] // Only riders can accept rides
    public async Task<IActionResult> Accept([FromBody] RideIdRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var riderId = RequireUserId();
            var result = await _rideService.AcceptAsync(riderId, request.RideId, cancellationToken);
            return Ok(ApiResponse<RideSummaryDto>.SuccessResponse("Ride accepted.", result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpPost("arrive")]
    [Authorize(Policy = "RequireRider")] // Only riders can mark arrival
    public async Task<IActionResult> Arrive([FromBody] RideIdRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var riderId = RequireUserId();
            var result = await _rideService.ArriveAsync(riderId, request.RideId, cancellationToken);
            return Ok(ApiResponse<RideSummaryDto>.SuccessResponse("Arrival recorded.", result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpPost("verify")]
    [Authorize(Policy = "RequireRider")] // Only riders can verify OTP to start a ride
    public async Task<IActionResult> Verify([FromBody] VerifyOtpRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var riderId = RequireUserId();
            var result = await _rideService.VerifyAsync(riderId, request.RideId, request.Otp, cancellationToken);
            return Ok(ApiResponse<RideSummaryDto>.SuccessResponse("Ride completed.", result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpPost("rider-complete")]
    [Authorize(Policy = "RequireRider")] // Only riders can mark their side complete
    public async Task<IActionResult> RiderComplete(
        [FromBody] RideIdRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var riderId = RequireUserId();
            var result = await _rideService.RiderCompleteAsync(riderId, request.RideId, cancellationToken);
            return Ok(ApiResponse<RideSummaryDto>.SuccessResponse(
                "Ride marked complete. Waiting for student confirmation.", result));
        }
        catch (UnauthorizedAccessException ex) {return Forbid(ex.Message);}
        catch (Exception ex) {return BadRequest(ApiResponse<object>.FailResponse(ex.Message));}
    }

    [HttpPost("student-confirm")]
    [Authorize(Policy = "RequireStudent")] // Only students can confirm ride completion
    public async Task<IActionResult> StudentConfirm(
        [FromBody] RideIdRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var studentId = RequireUserId();
            var (success, error) = await _rideService.StudentConfirmCompleteAsync(
                studentId, request.RideId, cancellationToken);

            if (!success)
                return BadRequest(ApiResponse<object>.FailResponse(error!));

            return Ok(ApiResponse<object>.SuccessResponse("Ride completed.", null));
        }
        catch (Exception ex) {return BadRequest(ApiResponse<object>.FailResponse(ex.Message));}
    }


    [HttpPost("otp/refresh")]
    [Authorize(Policy = "RequireStudent")] // Only students can refresh their OTP
    public async Task<IActionResult> RefreshOtp([FromBody] RideIdRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var studentId = RequireUserId();
            var result = await _rideService.RefreshOtpAsync(studentId, request.RideId, cancellationToken);
            return Ok(ApiResponse<RideSummaryDto>.SuccessResponse("OTP refreshed.", result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpPost("cancel")]
    [Authorize(Policy = "RequireStudent")] // Only students can cancel their request
    public async Task<IActionResult> Cancel([FromBody] RideIdRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var studentId = RequireUserId();
            var result = await _rideService.CancelAsync(studentId, request.RideId, cancellationToken);
            return Ok(ApiResponse<RideSummaryDto>.SuccessResponse("Ride cancelled.", result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpGet("rider/status")]
    [Authorize(Policy = "RequireRider")] // Only riders can fetch their availability & approval status
    public async Task<IActionResult> GetRiderStatus(CancellationToken cancellationToken)
    {
        try
        {
            var riderId = RequireUserId();
            var status = await _rideService.GetRiderStatusAsync(riderId, cancellationToken);
            if (status == null)
            {
                return NotFound(ApiResponse<object>.FailResponse("Rider status profile not found."));
            }
            return Ok(ApiResponse<object>.SuccessResponse("Rider status fetched.", new
            {
                riderId = status.RiderId,
                isOnline = status.IsOnline,
                approvalStatus = status.ApprovalStatus.ToString(),
                riderTier = status.RiderTier.ToString()
            }));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpGet("rider/stats")]
    [Authorize(Policy = "RequireRider")]
    public async Task<IActionResult> GetRiderStats(CancellationToken cancellationToken)
    {
        try
        {
            var riderId = RequireUserId();
            var stats = await _rideService.GetRiderStatsAsync(riderId, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse("Rider stats fetched.", stats));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpPut("rider/status")]
    [Authorize(Policy = "RequireRider")] // Only riders can toggle their availability
    public async Task<IActionResult> SetRiderStatus([FromBody] RiderStatusUpdateRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var riderId = RequireUserId();
            await _rideService.SetRiderStatusAsync(riderId, request.IsOnline, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse("Rider availability updated.", new { request.IsOnline }));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpPost("location/heartbeat")]
    [Authorize(Policy = "RequireRider")] // Only riders submit location heartbeats
    public async Task<IActionResult> LocationHeartbeat([FromBody] LocationHeartbeatRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var riderId = RequireUserId();
            await _rideService.SubmitHeartbeatAsync(riderId, request, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse("Heartbeat received.", new { request.RideId }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpGet("location/{rideId}")]
    [Authorize(Policy = "RequireStudent")] // Only students poll live location of their rider
    public async Task<IActionResult> GetLiveLocation(string rideId, CancellationToken cancellationToken)
    {
        try
        {
            var studentId = RequireUserId();
            var location = await _rideService.GetLiveLocationAsync(studentId, rideId, cancellationToken);
            return Ok(ApiResponse<RideLocationResponseDto>.SuccessResponse("Live location fetched.", location));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    private string RequireUserId()
    {
        var userId = User.FindFirstValue("userId");
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        return userId;
    }
}
