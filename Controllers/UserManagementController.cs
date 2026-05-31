using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using System.ComponentModel.DataAnnotations;

namespace NearU_Backend_Revised.Controllers;

/// <summary>
/// Admin-only endpoints for managing platform users.
/// All routes require the "Admin" role.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public class UserManagementController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(ApplicationDbContext db, ILogger<UserManagementController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── List Users ───────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/admin/users — paginated list of all users with optional role and status filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] string? role = null,
        [FromQuery] int? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsQueryable();

        // Filter by role
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        // Filter by active status
        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        // Search by username or email
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.Username.Contains(search) ||
                u.Email.Contains(search));

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                userId        = u.Id,
                username      = u.Username,
                email         = u.Email,
                role          = u.Role,
                isActive      = u.IsActive,
                mobileNumber  = u.MobileNumber,
                createdDate   = u.CreatedDate,
                lastLoginDate = u.LastLoginDate
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse("Users fetched.", new { total, page, pageSize, users }));
    }

    // ─── Get Single User ──────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/admin/users/{userId} — full profile of a specific user.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<object>.FailResponse($"User {userId} not found."));

        return Ok(ApiResponse<object>.SuccessResponse("User fetched.", new
        {
            userId        = user.Id,
            username      = user.Username,
            email         = user.Email,
            role          = user.Role,
            isActive      = user.IsActive,
            mobileNumber  = user.MobileNumber,
            studentId     = user.StudentId,
            faculty       = user.Faculty,
            year          = user.Year,
            address       = user.Address,
            city          = user.City,
            dateOfBirth   = user.DateOfBirth,
            profilePicture = user.ProfilePictureUrl,
            createdDate   = user.CreatedDate,
            lastLoginDate = user.LastLoginDate
        }));
    }

    // ─── Activate / Deactivate ────────────────────────────────────────────────

    /// <summary>
    /// PUT /api/admin/users/{userId}/activate — reactivates a deactivated account.
    /// </summary>
    [HttpPut("{userId}/activate")]
    public async Task<IActionResult> ActivateUser(string userId, CancellationToken cancellationToken)
        => await SetActiveStatus(userId, active: 1, cancellationToken);

    /// <summary>
    /// PUT /api/admin/users/{userId}/deactivate — deactivates a user account (soft delete).
    /// </summary>
    [HttpPut("{userId}/deactivate")]
    public async Task<IActionResult> DeactivateUser(string userId, CancellationToken cancellationToken)
        => await SetActiveStatus(userId, active: 0, cancellationToken);

    // ─── Change Role ──────────────────────────────────────────────────────────

    /// <summary>
    /// PUT /api/admin/users/{userId}/role — promotes or demotes a user's role.
    /// Body: { "role": "Student" | "Rider" | "Business" | "Admin" }
    /// </summary>
    [HttpPut("{userId}/role")]
    public async Task<IActionResult> ChangeRole(
        string userId,
        [FromBody] ChangeUserRoleDto request,
        CancellationToken cancellationToken)
    {
        if (!UserRoles.IsValidRole(request.Role))
            return BadRequest(ApiResponse<object>.FailResponse(
                $"Invalid role '{request.Role}'. Valid roles: {string.Join(", ", UserRoles.AllRoles)}"));

        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<object>.FailResponse($"User {userId} not found."));

        // Prevent an admin from accidentally stripping the last admin account
        if (user.Role == UserRoles.Admin && request.Role != UserRoles.Admin)
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == UserRoles.Admin, cancellationToken);
            if (adminCount <= 1)
                return BadRequest(ApiResponse<object>.FailResponse(
                    "Cannot demote the last admin account. Promote another user first."));
        }

        var previousRole = user.Role;
        user.Role = request.Role;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin changed user {UserId} role from {Previous} to {New}",
            userId, previousRole, request.Role);

        return Ok(ApiResponse<object>.SuccessResponse(
            $"User role updated to {request.Role}.",
            new { userId, previousRole, newRole = request.Role }));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<IActionResult> SetActiveStatus(
        string userId, int active, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<object>.FailResponse($"User {userId} not found."));

        user.IsActive = active;
        await _db.SaveChangesAsync(cancellationToken);

        var action = active == 1 ? "activated" : "deactivated";
        _logger.LogInformation("Admin {Action} user {UserId}", action, userId);

        return Ok(ApiResponse<object>.SuccessResponse($"User {action} successfully.", new { userId, isActive = active }));
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public class ChangeUserRoleDto
{
    [Required]
    public string Role { get; set; } = string.Empty;
}
