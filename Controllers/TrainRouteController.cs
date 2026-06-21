using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainRoutesController : ControllerBase
    {
        private readonly ITrainRouteService _service;

        public TrainRoutesController(ITrainRouteService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var routes = await _service.GetAllAsync();
            return Ok(routes);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var route = await _service.GetByIdAsync(id);
            if (route == null) return NotFound(new { message = "Train route not found." });
            return Ok(route);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] TrainRouteUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] TrainRouteUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound(new { message = "Train route not found." });
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = "Train route not found." });
            return NoContent();
        }
    }
}