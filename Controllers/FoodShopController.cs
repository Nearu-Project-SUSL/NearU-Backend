using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs.FoodShop;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.Enums;

namespace NearU_Backend_Revised.Controllers
{
    [ApiController] //validate incoming req and return 400 if bad
    [Route("api/foodshops")] //base route

    public class FoodShopController : ControllerBase
    {
        private readonly IFoodShopService _service;

        public FoodShopController(IFoodShopService service)
        {
            _service = service;
        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            return Ok(FoodCategory.AllowedCategories);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1, //read values from url
            [FromQuery] int pageSize = 9,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null
        )
        {
            if (page < 1) page = 1; //page num can not be less than 1

            if (pageSize < 1) pageSize = 9;
            if (pageSize > 50) pageSize = 50;
            
            var result = await _service.GetAllShopsAsync(page, pageSize, category, search);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var shop = await _service.GetShopByIdAsync(id);
            if (shop == null)
                return NotFound(new { message = "Shop not found" });
            return Ok(shop);
        }

        [HttpPost]
        [Consumes("multipart/form-data")] //accept form data for image upload not json
        public async Task<IActionResult> Create([FromForm] CreateFoodShop request)
        {
            var shop = await _service.CreateShopAsync(request);

            if (shop == null)
                return StatusCode(500, new { message = "Failed to create shop" });

            return CreatedAtAction(nameof(GetById), new { id = shop.Id }, shop);
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(string id, [FromForm] UpdateFoodShop request)
        {
            var shop = await _service.UpdateShopAsync(id, request);
            if (shop == null)
                return NotFound(new { message = "Shop not found" });
            return Ok(shop);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteShopAsync(id);
            if (!deleted)
                return NotFound(new { message = "Shop not found" });
            return NoContent(); //204 successful delete
        }
    }
        
}