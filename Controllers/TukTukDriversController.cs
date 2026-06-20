using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TukTukDriversController : ControllerBase
    {
        private readonly ITukTukDriverService _service;

        public TukTukDriversController(ITukTukDriverService service)
        {
            _service = service;
        }

        // Visible to any authenticated user (students)
        [HttpGet]
        [AllowAnonymous]

        public async Task<IActionResult> GetAll()
        {
            var drivers = await _service.GetAllAsync();
            return Ok(drivers);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var driver = await _service.GetByIdAsync(id);
            if (driver == null) return NotFound(new { message = "Tuk tuk driver not found." });
            return Ok(driver);
        }

        // Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] TukTukDriverUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // Admin only
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] TukTukDriverUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound(new { message = "Tuk tuk driver not found." });
            return Ok(updated);
        }

        // Admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = "Tuk tuk driver not found." });
            return NoContent();
        }
    }
}