using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs.Accommodation;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Controllers
{
    [ApiController] //validate incoming req and return 400 if bad
    [Route("api/accommodations")] //base route

    public class AccommodationController : ControllerBase
    {
        private readonly IAccommodationService _service;

        public AccommodationController(IAccommodationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var accommodations = await _service.GetAllAccommodationsAsync();
            return Ok(accommodations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var accommodation = await _service.GetAccommodationByIdAsync(id);
            if (accommodation == null)
                return NotFound(new { message = "Accommodation not found" });
            return Ok(accommodation);
        }

        [HttpPost]
        [Consumes("multipart/form-data")] //accept form data for image upload not json
        public async Task<IActionResult> Create([FromForm] CreateAccommodation request)
        {
            var accommodation = await _service.CreateAccommodationAsync(request);

            if (accommodation == null)
                return StatusCode(500, new { message = "Failed to create accommodation" });

            return CreatedAtAction(nameof(GetById), new { id = accommodation.Id }, accommodation);
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(string id, [FromForm] UpdateAccommodation request)
        {
            var accommodation = await _service.UpdateAccommodationAsync(id, request);
            if (accommodation == null)
                return NotFound(new { message = "Accommodation not found" });
            return Ok(accommodation);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAccommodationAsync(id);
            if (!deleted)
                return NotFound(new { message = "Accommodation not found" });
            return NoContent(); //204 successful delete
        }
    }

}
