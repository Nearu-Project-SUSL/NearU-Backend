using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.DTOs.Business;
using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Controllers;

[ApiController]
[Route("api/business-applications")]
[Authorize]
public class BusinessApplicationController : ControllerBase
{
  public readonly ApplicationDbContext _db;

  public BusinessApplicationController(ApplicationDbContext db)
  {
    _db = db;
  }

  [HttpPost]
  public async Task<IActionResult> Submit(
    [FromBody] CreateBusinessApplication dto)
  {
    var userId = User.FindFirst("userId")?.Value;

    if(string.IsNullOrEmpty(userId))
      return Unauthorized();

    var application = new BusinessApplication
    {
      UserId = userId,
      BusinessType = dto.BusinessType,
      BusinessName = dto.BusinessName,
      OwnerName = dto.OwnerName,
      Phone = dto.Phone,
      Address = dto.Address,
      Description = dto.Description,
      RegistrationNumber = dto.RegistrationNumber,
      ApplicationDataJson = dto.ApplicationDataJson,
      Status = "Pending"
    };

    _db.BusinessApplications.Add(application);

    await _db.SaveChangesAsync();

    return Ok(
      ApiResponse<object>.SuccessResponse(
        "Application submitted successfully",
        application.UserId
      )
      
    );
  } 
}