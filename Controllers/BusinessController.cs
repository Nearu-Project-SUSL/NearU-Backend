using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.DTOs.FoodShop;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;

[ApiController]
[Authorize(Roles = "Business")]
[Route("api/business")]
public class BusinessController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFoodShopService _foodShopService;
    private readonly ILogger<BusinessController> _logger;

    public BusinessController(
        ApplicationDbContext dbContext,
        IFoodShopService foodShopService,
        ILogger<BusinessController> logger)
    {
        _dbContext = dbContext;
        _foodShopService = foodShopService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/business/status
    /// Business owner checks their application status (Pending/Approved/Rejected).
    /// Frontend uses this to decide whether to show the category form.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetApplicationStatus(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("User not found in token."));

        var application = await _dbContext.BusinessApplications
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.Id,
                a.BusinessName,
                a.BusinessType,
                a.Status,
                a.SubmittedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (application is null)
            return NotFound(ApiResponse<object>.FailResponse("No application found."));

        return Ok(ApiResponse<object>.SuccessResponse("Application status fetched.", application));
    }

    /// <summary>
    /// POST /api/business/food
    /// Called after admin approves — reuses existing CreateFoodShop DTO and service.
    /// Address and Phone are pre-filled from their application if not provided.
    /// </summary>
    [HttpPost("food")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateFoodShopProfile(
        [FromForm] CreateFoodShop request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("User not found in token."));

        // 1. Must have an approved Food application
        var application = await _dbContext.BusinessApplications
            .FirstOrDefaultAsync(a =>
                a.UserId == userId &&
                a.Status == "Approved" &&
                a.BusinessType.ToLower() == "food", cancellationToken);

        if (application is null)
            return BadRequest(ApiResponse<object>.FailResponse(
                "No approved Food application found. Please wait for admin approval."));

        // 2. Prevent duplicate shop
        var alreadyExists = await _dbContext.FoodShops
            .AnyAsync(f => f.OwnerId == userId, cancellationToken);

        if (alreadyExists)
            return Conflict(ApiResponse<object>.FailResponse(
                "You already have a food shop profile."));

        // 3. Pre-fill Address and Phone from application if user left them blank
        request.Address     ??= application.Address;
        request.PhoneNumber ??= application.Phone;
        request.OwnerId = userId;

        // 4. Reuse your existing service — no duplicate logic
        var shop = await _foodShopService.CreateShopAsync(request);

        if (shop is null)
            return StatusCode(500, ApiResponse<object>.FailResponse("Failed to create food shop."));

        _logger.LogInformation("FoodShop created for user {UserId}: {ShopName}", userId, shop.Id);

        return Ok(ApiResponse<object>.SuccessResponse(
            "Food shop profile created successfully.", shop));
    }

    /// <summary>
    /// GET /api/business/food/me
    /// Business owner views their own shop.
    /// </summary>
    [HttpGet("food/me")]
    public async Task<IActionResult> GetMyFoodShop(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("User not found in token."));

        var shop = await _dbContext.FoodShops
            .Where(f => f.OwnerId == userId)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Category,
                f.Address,
                f.PhoneNumber,
                f.Description,
                f.PhotoUrl,
                f.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (shop is null)
            return NotFound(ApiResponse<object>.FailResponse(
                "No food shop profile found. Please complete your business profile."));

        return Ok(ApiResponse<object>.SuccessResponse("Food shop fetched.", shop));
    }
}