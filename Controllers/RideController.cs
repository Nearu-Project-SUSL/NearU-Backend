using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.DTOs.Rides;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend_Revised.Controllers
{
  [ApiController]
  [Route("api/rides")]
  public class RideController : ControllerBase
  {
    private readonly IRideService _service;

    public RideController(IRideService service)
    {
      _service = service;
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRideRequestDto request)
    {
      var (ride, error) = await _service.CreateRideAsync(request);

      if(error != null)
        return BadRequest(new {message = error});

      return CreatedAtAction(nameof(CreateRequest), new {id = ride!.Id}, ride);
    }

    [HttpPost("accept")]
    public async Task<IActionResult> AcceptRequest([FromBody] AcceptRideDto request)
    {
      var (ride, error) = await _service.AcceptRideAsync(request);

      if(error != null)
        return Conflict(new {message = error});

      return Ok(ride);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request)
    {
      var (success, error) = await _service.VerifyOtpAsync(request);

      if(!success)
        return BadRequest(new {message = error});

      return Ok(new {message = "Ride confirmed"});
    }
  }
}