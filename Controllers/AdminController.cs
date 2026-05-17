using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Enums;
using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Controllers;

/// <summary>
/// Admin-only endpoints for platform management.
/// All routes require the "Admin" role.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext dbContext, ILogger<AdminController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // ─── Rider Approval ───────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/admin/riders — lists all rider applications with their approval status.
    /// Optionally filter by status: ?status=Pending
    /// </summary>
    [HttpGet("riders")]
    public async Task<IActionResult> GetRiders(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RiderStatuses
            .Include(rs => rs.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RiderApprovalStatus>(status, true, out var parsedStatus))
            query = query.Where(rs => rs.ApprovalStatus == parsedStatus);

        var total = await query.CountAsync(cancellationToken);
        var riders = await query
            .OrderByDescending(rs => rs.LastSeen)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rs => new
            {
                riderId       = rs.RiderId,
                name          = rs.User.Username,
                email         = rs.User.Email,
                approvalStatus = rs.ApprovalStatus.ToString(),
                riderTier     = rs.RiderTier.ToString(),
                isOnline      = rs.IsOnline,
                lastSeen      = rs.LastSeen
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse("Riders fetched.", new { total, page, pageSize, riders }));
    }

    /// <summary>
    /// PUT /api/admin/riders/{riderId}/approve — approves a pending rider application.
    /// </summary>
    [HttpPut("riders/{riderId}/approve")]
    public async Task<IActionResult> ApproveRider(string riderId, CancellationToken cancellationToken)
    {
        return await SetApprovalStatus(riderId, RiderApprovalStatus.Approved, cancellationToken);
    }

    /// <summary>
    /// PUT /api/admin/riders/{riderId}/reject — rejects a rider application.
    /// </summary>
    [HttpPut("riders/{riderId}/reject")]
    public async Task<IActionResult> RejectRider(string riderId, CancellationToken cancellationToken)
    {
        return await SetApprovalStatus(riderId, RiderApprovalStatus.Rejected, cancellationToken);
    }

    /// <summary>
    /// PUT /api/admin/riders/{riderId}/suspend — suspends an approved rider.
    /// </summary>
    [HttpPut("riders/{riderId}/suspend")]
    public async Task<IActionResult> SuspendRider(string riderId, CancellationToken cancellationToken)
    {
        return await SetApprovalStatus(riderId, RiderApprovalStatus.Suspended, cancellationToken);
    }

    /// <summary>
    /// PUT /api/admin/riders/{riderId}/tier — changes a rider's service tier (Standard/Premium).
    /// Body: { "tier": "Premium" }
    /// </summary>
    [HttpPut("riders/{riderId}/tier")]
    public async Task<IActionResult> SetRiderTier(string riderId, [FromBody] SetRiderTierDto request, CancellationToken cancellationToken)
    {
        var riderStatus = await _dbContext.RiderStatuses
            .FirstOrDefaultAsync(rs => rs.RiderId == riderId, cancellationToken);

        if (riderStatus is null)
            return NotFound(ApiResponse<object>.FailResponse($"Rider {riderId} not found."));

        riderStatus.RiderTier = request.Tier;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin set rider {RiderId} tier to {Tier}", riderId, request.Tier);
        return Ok(ApiResponse<object>.SuccessResponse($"Rider tier updated to {request.Tier}.", new { riderId, tier = request.Tier.ToString() }));
    }

    // ─── Platform Stats ───────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/admin/stats — high-level platform statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var totalRides   = await _dbContext.RideRequests.CountAsync(cancellationToken);
        var activeRides  = await _dbContext.RideRequests
            .CountAsync(r => r.Status != RideRequestStatus.Completed
                          && r.Status != RideRequestStatus.Cancelled
                          && r.Status != RideRequestStatus.Expired, cancellationToken);
        var totalRiders  = await _dbContext.RiderStatuses.CountAsync(cancellationToken);
        var onlineRiders = await _dbContext.RiderStatuses.CountAsync(rs => rs.IsOnline, cancellationToken);
        var pendingApprovals = await _dbContext.RiderStatuses
            .CountAsync(rs => rs.ApprovalStatus == RiderApprovalStatus.Pending, cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse("Stats fetched.", new
        {
            totalRides,
            activeRides,
            totalRiders,
            onlineRiders,
            pendingApprovals
        }));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<IActionResult> SetApprovalStatus(
        string riderId,
        RiderApprovalStatus newStatus,
        CancellationToken cancellationToken)
    {
        var riderStatus = await _dbContext.RiderStatuses
            .FirstOrDefaultAsync(rs => rs.RiderId == riderId, cancellationToken);

        if (riderStatus is null)
            return NotFound(ApiResponse<object>.FailResponse($"Rider {riderId} not found."));

        var previousStatus = riderStatus.ApprovalStatus;
        riderStatus.ApprovalStatus = newStatus;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin changed rider {RiderId} status from {Previous} to {New}",
            riderId, previousStatus, newStatus);

        return Ok(ApiResponse<object>.SuccessResponse(
            $"Rider {newStatus.ToString().ToLower()} successfully.",
            new { riderId, status = newStatus.ToString() }));
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public class SetRiderTierDto
{
    public RiderTier Tier { get; set; }
}
