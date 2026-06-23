using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.DTOs.Deal;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Controllers
{
    [ApiController]
    [Route("api/deals")]
    public class DealsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;

        public DealsController(ApplicationDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        // 1. GET /api/deals (Public - Retrieve all approved deals)
        [HttpGet]
        public async Task<IActionResult> GetApprovedDeals()
        {
            try
            {
                var now = DateTime.UtcNow;
                var deals = await _context.Deals
                    .Where(d => d.ApprovalStatus == "Approved" && (d.ValidTo == null || d.ValidTo >= now))
                    .OrderByDescending(d => d.CreatedAt)
                    .Include(d => d.SubmittedByUser)
                    .ToListAsync();

                var responseDto = deals.Select(d => MapToDto(d)).ToList();
                return Ok(ApiResponse<List<DealResponseDto>>.SuccessResponse("Approved deals retrieved successfully", responseDto));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        // 2. GET /api/deals/my (Authenticated Business Owners - Retrieve their own deals)
        [HttpGet("my")]
        [Authorize(Policy = "RequireBusinessOrAdmin")]
        public async Task<IActionResult> GetMyDeals()
        {
            try
            {
                var userId = User.FindFirstValue("userId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<object>.FailResponse("User not authenticated"));

                var deals = await _context.Deals
                    .Where(d => d.SubmittedByUserId == userId)
                    .OrderByDescending(d => d.CreatedAt)
                    .Include(d => d.SubmittedByUser)
                    .ToListAsync();

                var responseDto = deals.Select(d => MapToDto(d)).ToList();
                return Ok(ApiResponse<List<DealResponseDto>>.SuccessResponse("Your deals retrieved successfully", responseDto));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        // 3. POST /api/deals (Authenticated Business Owners - Create/Submit a deal)
        [HttpPost]
        [Authorize(Policy = "RequireBusinessOrAdmin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateDeal([FromForm] CreateDealDto dto)
        {
            try
            {
                var userId = User.FindFirstValue("userId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<object>.FailResponse("User not authenticated"));

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return BadRequest(ApiResponse<object>.FailResponse("User not found"));

                string? imageUrl = null;
                if (dto.Image != null && dto.Image.Length > 0)
                {
                    imageUrl = await _imageService.UploadImageAsync(dto.Image, "deals");
                }

                var deal = new Deal
                {
                    Id = Guid.NewGuid().ToString(),
                    ShopName = dto.ShopName,
                    ShopType = dto.ShopType,
                    Title = dto.Title,
                    Description = dto.Description,
                    BadgeText = dto.BadgeText,
                    BadgeColor = dto.BadgeColor,
                    ImageUrl = imageUrl,
                    ValidFrom = dto.ValidFrom.HasValue ? DateTime.SpecifyKind(dto.ValidFrom.Value, DateTimeKind.Utc) : null,
                    ValidTo = dto.ValidTo.HasValue ? DateTime.SpecifyKind(dto.ValidTo.Value, DateTimeKind.Utc) : null,
                    SubmittedByUserId = userId,
                    SubmittedByUser = user,
                    ApprovalStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Deals.Add(deal);
                await _context.SaveChangesAsync();

                var responseDto = MapToDto(deal);
                return Created($"/api/deals/{deal.Id}", ApiResponse<DealResponseDto>.SuccessResponse("Deal application submitted for review", responseDto));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        // 4. GET /api/admin/deals (Admin Console - Review submitted deals)
        [HttpGet("/api/admin/deals")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> GetAdminDeals([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Deals.AsQueryable();
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(d => d.ApprovalStatus == status);
                }

                var total = await query.CountAsync();
                var deals = await query
                    .OrderByDescending(d => d.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(d => d.SubmittedByUser)
                    .ToListAsync();

                var responseDto = deals.Select(d => MapToDto(d)).ToList();

                var listResponse = new AdminDealsListResponse
                {
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    Deals = responseDto
                };

                return Ok(ApiResponse<AdminDealsListResponse>.SuccessResponse("Admin deals list retrieved successfully", listResponse));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        // 5. PUT /api/admin/deals/{id}/approve (Admin Console - Approve deal)
        [HttpPut("/api/admin/deals/{id}/approve")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ApproveDeal(string id)
        {
            try
            {
                var deal = await _context.Deals.Include(d => d.SubmittedByUser).FirstOrDefaultAsync(d => d.Id == id);
                if (deal == null)
                    return NotFound(ApiResponse<object>.FailResponse("Deal not found"));

                deal.ApprovalStatus = "Approved";
                deal.RejectionReason = null;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<DealResponseDto>.SuccessResponse("Deal approved successfully", MapToDto(deal)));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        // 6. PUT /api/admin/deals/{id}/reject (Admin Console - Reject deal)
        [HttpPut("/api/admin/deals/{id}/reject")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> RejectDeal(string id, [FromBody] RejectDealRequest request)
        {
            try
            {
                var deal = await _context.Deals.Include(d => d.SubmittedByUser).FirstOrDefaultAsync(d => d.Id == id);
                if (deal == null)
                    return NotFound(ApiResponse<object>.FailResponse("Deal not found"));

                deal.ApprovalStatus = "Rejected";
                deal.RejectionReason = request.Reason ?? "Does not meet community guidelines";
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<DealResponseDto>.SuccessResponse("Deal rejected successfully", MapToDto(deal)));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        private static DealResponseDto MapToDto(Deal deal)
        {
            return new DealResponseDto
            {
                Id = deal.Id,
                ShopName = deal.ShopName,
                ShopType = deal.ShopType,
                Title = deal.Title,
                Description = deal.Description,
                BadgeText = deal.BadgeText,
                BadgeColor = deal.BadgeColor,
                ImageUrl = deal.ImageUrl,
                ValidFrom = deal.ValidFrom,
                ValidTo = deal.ValidTo,
                SubmittedByUserId = deal.SubmittedByUserId,
                SubmittedByName = deal.SubmittedByUser?.Username ?? "Unknown Business",
                ShopAddress = deal.SubmittedByUser?.Address,
                ApprovalStatus = deal.ApprovalStatus,
                RejectionReason = deal.RejectionReason,
                CreatedAt = deal.CreatedAt
            };
        }

        // 7. DELETE /api/deals/{id} (Authenticated Business Owners / Admins - Delete a deal)
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireBusinessOrAdmin")]
        public async Task<IActionResult> DeleteDeal(string id)
        {
            try
            {
                var userId = User.FindFirstValue("userId");
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<object>.FailResponse("User not authenticated"));

                var deal = await _context.Deals.FirstOrDefaultAsync(d => d.Id == id);
                if (deal == null)
                    return NotFound(ApiResponse<object>.FailResponse("Deal not found"));

                // Logic: Admins can delete any deal, Business Owners can only delete their own deals
                if (userRole != "Admin" && deal.SubmittedByUserId != userId)
                {
                    return StatusCode(403, ApiResponse<object>.FailResponse("You do not have permission to delete this deal"));
                }

                _context.Deals.Remove(deal);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse("Deal deleted successfully", null));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }
    }

    public class DealResponseDto
    {
        public string Id { get; set; } = null!;
        public string ShopName { get; set; } = null!;
        public string ShopType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string BadgeText { get; set; } = null!;
        public string BadgeColor { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string SubmittedByUserId { get; set; } = null!;
        public string SubmittedByName { get; set; } = null!;
        public string? ShopAddress { get; set; }
        public string ApprovalStatus { get; set; } = null!;
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminDealsListResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<DealResponseDto> Deals { get; set; } = null!;
    }

    public class RejectDealRequest
    {
        public string? Reason { get; set; }
    }
}
