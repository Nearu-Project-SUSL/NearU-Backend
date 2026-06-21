using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.Services;
using NearU_Backend_Revised.DTOs.User;
using NearU_Backend_Revised.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace NearU_Backend_Revised.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All user operations require authentication
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserProfile(string id)
        {
            try
            {
                // Verify that the user is requesting their own profile or is an admin
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserId != id && role != "Admin" && role != "SuperAdmin")
                {
                    return Forbid();
                }

                var user = await _userService.GetUserById(id);
                if (user == null)
                    return NotFound(ApiResponse<object>.FailResponse("User not found"));

                var data = new UserProfileResponse
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedDate = user.CreatedDate,
                    LastLoginDate = user.LastLoginDate,
                    IsActive = user.IsActive,
                    MobileNumber = user.MobileNumber,
                    StudentId = user.StudentId,
                    Faculty = user.Faculty,
                    Year = user.Year,
                    Address = user.Address,
                    City = user.City,
                    DateOfBirth = user.DateOfBirth,
                    ProfilePictureUrl = user.ProfilePictureUrl
                };

                return Ok(ApiResponse<object>.SuccessResponse("User retrieved successfully", data));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPut("{id}/profile")]
        public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId != id)
                {
                    return Forbid();
                }

                var updatedUser = await _userService.UpdateProfileAsync(id, request);
                
                var data = new UserProfileResponse
                {
                    UserId = updatedUser.Id,
                    Username = updatedUser.Username,
                    Email = updatedUser.Email,
                    Role = updatedUser.Role,
                    CreatedDate = updatedUser.CreatedDate,
                    LastLoginDate = updatedUser.LastLoginDate,
                    IsActive = updatedUser.IsActive,
                    MobileNumber = updatedUser.MobileNumber,
                    StudentId = updatedUser.StudentId,
                    Faculty = updatedUser.Faculty,
                    Year = updatedUser.Year,
                    Address = updatedUser.Address,
                    City = updatedUser.City,
                    DateOfBirth = updatedUser.DateOfBirth,
                    ProfilePictureUrl = updatedUser.ProfilePictureUrl
                };

                return Ok(ApiResponse<object>.SuccessResponse("Profile updated successfully", data));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("{id}/profile-picture")]
        public async Task<IActionResult> UploadProfilePicture(string id, IFormFile file)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId != id)
                {
                    return Forbid();
                }

                var updatedUser = await _userService.UpdateProfilePictureAsync(id, file);
                
                return Ok(ApiResponse<object>.SuccessResponse("Profile picture updated successfully", new { 
                    ProfilePictureUrl = updatedUser.ProfilePictureUrl 
                }));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(string id, [FromBody] DeleteAccountRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserId != id && role != "Admin" && role != "SuperAdmin")
                {
                    return Forbid();
                }

                if (string.IsNullOrEmpty(request?.Password))
                {
                    return BadRequest(ApiResponse<object>.FailResponse("Password is required to delete the account."));
                }

                await _userService.DeleteAccountAsync(id, request.Password);
                return Ok(ApiResponse<object>.SuccessResponse("Account deleted successfully", null));
            }
            catch (Exception ex)
            {
                if (ex.Message == "Incorrect password." || ex.Message == "User not found.")
                {
                    return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
                }

                // Log the full technical error to standard output (Dozzle logs) for backend developers
                Console.WriteLine($"[ERROR] Secure Account Deletion failed for User ID {id}: {ex}");

                return BadRequest(ApiResponse<object>.FailResponse("An unexpected database error occurred on the server while deleting your account. Please try again later or contact support."));
            }
        }
    }
}
