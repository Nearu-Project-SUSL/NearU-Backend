using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs.Photographer;
using NearU_Backend_Revised.DTOs.PhotographyPackage;
using NearU_Backend_Revised.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NearU_Backend_Revised.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotographersController : ControllerBase
    {
        private readonly IPhotographerService _photographerService;

        public PhotographersController(IPhotographerService photographerService)
        {
            _photographerService = photographerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? keyword, [FromQuery] string? location, [FromQuery] bool? isActive)
        {
            try
            {
                var photographers = await _photographerService.GetAllAsync(keyword, location, isActive);
                return Ok(photographers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _photographerService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Photographer profile not found." });

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "RequireBusinessOrAdmin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreatePhotographerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var created = await _photographerService.CreatePhotographerAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "RequireBusinessOrAdmin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdatePhotographerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Double check photographer ownership or admin permission
            var photographer = await _photographerService.GetByIdAsync(id);
            if (photographer == null)
                return NotFound(new { message = "Photographer profile not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && photographer.OwnerId != userId)
                return Forbid();

            var updated = await _photographerService.UpdatePhotographerAsync(id, dto);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "RequireBusinessOrAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var photographer = await _photographerService.GetByIdAsync(id);
            if (photographer == null)
                return NotFound(new { message = "Photographer profile not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && photographer.OwnerId != userId)
                return Forbid();

            var deleted = await _photographerService.DeletePhotographerAsync(id);
            if (!deleted)
                return BadRequest(new { message = "Failed to delete photographer profile." });

            return Ok(new { message = "Photographer profile deleted successfully." });
        }

        [HttpPost("{photographerId:guid}/packages")]
        [Authorize(Policy = "RequireBusinessOrAdmin")]
        public async Task<IActionResult> AddPackage(Guid photographerId, [FromBody] CreatePhotographyPackageDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var photographer = await _photographerService.GetByIdAsync(photographerId);
            if (photographer == null)
                return NotFound(new { message = "Photographer profile not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && photographer.OwnerId != userId)
                return Forbid();

            var created = await _photographerService.AddPackageAsync(photographerId, dto);
            return Ok(created);
        }

        [HttpPut("packages/{packageId:guid}")]
        [Authorize(Policy = "RequireBusinessOrAdmin")]
        public async Task<IActionResult> UpdatePackage(Guid packageId, [FromBody] UpdatePhotographyPackageDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _photographerService.UpdatePackageAsync(packageId, dto);
            if (updated == null)
                return NotFound(new { message = "Photography package not found." });

            return Ok(updated);
        }

        [HttpDelete("packages/{packageId:guid}")]
        [Authorize(Policy = "RequireBusinessOrAdmin")]
        public async Task<IActionResult> DeletePackage(Guid packageId)
        {
            var deleted = await _photographerService.DeletePackageAsync(packageId);
            if (!deleted)
                return NotFound(new { message = "Photography package not found." });

            return Ok(new { message = "Photography package deleted successfully." });
        }
    }
}
