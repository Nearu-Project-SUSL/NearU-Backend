using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs;
using NearU_Backend_Revised.Services.Interfaces;
using System.Security.Claims;

namespace NearU_Backend_Revised.Controllers
{
  [ApiController]
  [Route("api/testimonials")]
  public class TestimonialsController : ControllerBase
  {
    private readonly ITestimonialService _service;

    public TestimonialsController(ITestimonialService service)
    {
      _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
      var result = await _service.GetAllApprovedAsync();
      return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTestimonialDto dto)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirstValue("userId");
      if (string.IsNullOrEmpty(userId))
        return Unauthorized(new { Message = "Please log in to share your experience!" });

      var testimonial = await _service.CreateAsync(userId, dto.Message, dto.Rating);

      if (testimonial == null)
        return StatusCode(500, new { message = "Failed to save testimonial" });

      return Ok(new { Message = "Thank you for sharing your experience!", id = testimonial.Id });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirstValue("userId");
      if (string.IsNullOrEmpty(userId))
        return Unauthorized();

      try
      {
        var result = await _service.DeleteAsync(id, userId);
        return result ? Ok(new { message = "Deleted successfully" }) : NotFound();
      }
      catch (UnauthorizedAccessException)
      {
        return Forbid();
      }
    }
  }
}

