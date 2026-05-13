using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs.GiftProduct;
using NearU_Backend_Revised.DTOs.GiftShop;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GiftShopsController : ControllerBase
    {
        private readonly IGiftShopService _giftShopService;

        public GiftShopsController(IGiftShopService giftShopService)
        {
            _giftShopService = giftShopService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] string? location,
            [FromQuery] bool? isActive)
        {
            var result = await _giftShopService.GetAllAsync(keyword, location, isActive);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _giftShopService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Gift shop not found." });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateGiftShopDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _giftShopService.CreateGiftShopAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdateGiftShopDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _giftShopService.UpdateGiftShopAsync(id, dto);
            if (updated == null)
                return NotFound(new { message = "Gift shop not found." });

            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _giftShopService.DeleteGiftShopAsync(id);
            if (!deleted)
                return NotFound(new { message = "Gift shop not found." });

            return Ok(new { message = "Gift shop deleted successfully." });
        }

        [HttpPost("{giftShopId:guid}/products")]
        public async Task<IActionResult> AddProduct(Guid giftShopId, [FromForm] CreateGiftProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _giftShopService.AddProductAsync(giftShopId, dto);
            if (created == null)
                return NotFound(new { message = "Gift shop not found." });

            return Ok(created);
        }

        [HttpPut("products/{productId:guid}")]
        public async Task<IActionResult> UpdateProduct(Guid productId, [FromForm] UpdateGiftProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _giftShopService.UpdateProductAsync(productId, dto);
            if (updated == null)
                return NotFound(new { message = "Product not found." });

            return Ok(updated);
        }

        [HttpDelete("products/{productId:guid}")]
        public async Task<IActionResult> DeleteProduct(Guid productId)
        {
            var deleted = await _giftShopService.DeleteProductAsync(productId);
            if (!deleted)
                return NotFound(new { message = "Product not found." });

            return Ok(new { message = "Product deleted successfully." });
        }
    }
}