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

    public RideController(IRideService rideService)
    {
        _rideService = rideService;
    }

    // ─── Fare Estimate ──────────────────────────────────────────────────────────

    /// <summary>GET /api/rides/estimate — no ride is created, pure fare calculation.</summary>
    [HttpGet("rides/estimate")]
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

    // ─── Ride Requests ───────────────────────────────────────────────────────────

    [HttpPost("requests")]
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

    [HttpPost("otp/refresh")]
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

    [HttpPut("rider/status")]
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
