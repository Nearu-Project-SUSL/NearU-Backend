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
        // ─── Self-Healing database check ───
        // Automatically initialize RiderStatus for any User registered as "Rider" but missing a status entry
        var missingRiders = await _dbContext.Users
            .Where(u => u.Role == "Rider" && !_dbContext.RiderStatuses.Any(rs => rs.RiderId == u.Id))
            .ToListAsync(cancellationToken);

        if (missingRiders.Any())
        {
            foreach (var user in missingRiders)
            {
                _dbContext.RiderStatuses.Add(new RiderStatus
                {
                    RiderId = user.Id,
                    IsOnline = false,
                    ApprovalStatus = RiderApprovalStatus.Pending,
                    LastSeen = DateTime.UtcNow
                });
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

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

    // ─── Business Application Approval ───────────────────────────────────────────

    /// <summary>
    /// GET /api/admin/businesses — list business applications, filter by status
    /// </summary>
    [HttpGet("businesses")]
    public async Task<IActionResult> GetBusinessApplications(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BusinessApplications
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        var total = await query.CountAsync(cancellationToken);

        var applications = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                ownerEmail   = a.User.Email,
                a.BusinessName,
                a.BusinessType,
                a.OwnerName,
                a.Phone,
                a.Address,
                a.Status,
                a.SubmittedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse("Business applications fetched.",
            new { total, page, pageSize, applications }));
    }

    /// <summary>
    /// PUT /api/admin/businesses/{id}/approve
    /// </summary>
    [HttpPut("businesses/{id}/approve")]
    public async Task<IActionResult> ApproveBusinessApplication(
        string id, CancellationToken cancellationToken)
    {
        var application = await _dbContext.BusinessApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (application is null)
            return NotFound(ApiResponse<object>.FailResponse("Application not found."));

        if (application.Status != "Pending")
            return BadRequest(ApiResponse<object>.FailResponse(
                $"Application is already {application.Status}."));

        // 1. Approve the application
        application.Status = "Approved";

        // 2. User already exists in DB — just confirm their role is "Business"
        //    (it was set at registration, this is just a safety check)
        application.User.Role = "Business";

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin approved business application {Id} for {Name}",
            id, application.BusinessName);

        return Ok(ApiResponse<object>.SuccessResponse(
            $"'{application.BusinessName}' approved. Owner can now fill their business profile.",
            new { id, status = "Approved" }));
    }

    /// <summary>
    /// PUT /api/admin/businesses/{id}/reject
    /// Body: { "reason": "..." }
    /// </summary>
    [HttpPut("businesses/{id}/reject")]
    public async Task<IActionResult> RejectBusinessApplication(
        string id, [FromBody] RejectBusinessDto request, CancellationToken cancellationToken)
    {
        var application = await _dbContext.BusinessApplications
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (application is null)
            return NotFound(ApiResponse<object>.FailResponse("Application not found."));

        if (application.Status != "Pending")
            return BadRequest(ApiResponse<object>.FailResponse(
                $"Application is already {application.Status}."));

        application.Status = "Rejected";

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin rejected business application {Id}. Reason: {Reason}",
            id, request.Reason);

        return Ok(ApiResponse<object>.SuccessResponse("Application rejected.",
            new { id, status = "Rejected", request.Reason }));
    }

    
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public class SetRiderTierDto
{
    public RiderTier Tier { get; set; }
}

public class RejectBusinessDto
{
    public string Reason { get; set; } = string.Empty;
}