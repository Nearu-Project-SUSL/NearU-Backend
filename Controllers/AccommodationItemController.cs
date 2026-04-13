using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs.AccommodationItem;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Controllers
{
    [ApiController]
    [Route("api/accommodations/{accommodationId}/items")]

    public class AccommodationItemController : ControllerBase
    {
        private readonly IAccommodationItemService _service;

        public AccommodationItemController(IAccommodationItemService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string accommodationId)
        {
            var items = await _service.GetItemsByAccommodationAsync(accommodationId);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string accommodationId, string id)
        {
            var item = await _service.GetItemByIdAsync(id);
            if (item == null)
                return NotFound(new { message = "Item not found" });

            if (item.AccommodationId != accommodationId)
                return NotFound(new { message = "Item not found in this accommodation" });

            return Ok(item);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create(string accommodationId, [FromForm] CreateAccommodationItem request)
        {
            var item = await _service.CreateItemAsync(accommodationId, request);
            if (item == null)
                return NotFound(new { message = "Accommodation not found" });
            return CreatedAtAction(nameof(GetById), new { accommodationId = accommodationId, id = item.Id }, item);
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(string accommodationId, string id, [FromForm] UpdateAccommodationItem request)
        {
            var existing = await _service.GetItemByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Item not found" });

            if (existing.AccommodationId != accommodationId)
                return NotFound(new { message = "Item not found in this accommodation" });

            var item = await _service.UpdateItemAsync(id, request);
            if (item == null)
                return NotFound(new { message = "Item not found" });

            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string accommodationId, string id)
        {
            var existing = await _service.GetItemByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Item not found" });

            if (existing.AccommodationId != accommodationId)
                return NotFound(new { message = "Item not found in this accommodation" });

            var deleted = await _service.DeleteItemAsync(id);
            if (!deleted)
                return NotFound(new { message = "Failed to delete item" });
            return NoContent();
        }
    }    
}
