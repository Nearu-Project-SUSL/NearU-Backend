using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.Services;
using NearU_Backend_Revised.DTOs.Auth;
using NearU_Backend_Revised.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.RateLimiting;


namespace NearU_Backend_Revised.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _userService.Register(request);

                object data = user.Role == "Business"
                    ? new
                    {
                        userId   = user.Id,
                        username = user.Username,
                        message  = "Registration submitted. Awaiting admin approval."
                    }
                    : new
                    {
                        userId   = user.Id,
                        username = user.Username,
                    };

                var message = user.Role == "Business"
                    ? "Business registration submitted."
                    : "User registered successfully";

                return Created(string.Empty, ApiResponse<object>.SuccessResponse(message, data));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("login-limit")]
        public async Task<IActionResult> Login([FromBody]LoginRequest request)
        {
            try
            {
                var authResponse = await _userService.Login(request);
                return Ok(ApiResponse<object>.SuccessResponse("Login successful", authResponse)); 
            }
            catch (Exception ex)
            {
                return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            try
            {
                var authResponse = await _userService.GoogleLoginAsync(request);
                return Ok(ApiResponse<object>.SuccessResponse("Google Login successful", authResponse));
            }
            catch (Exception ex)
            {
                return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var authResponse = await _userService.RefreshToken(request);
                return Ok(ApiResponse<object>.SuccessResponse("Token refreshed successfully", authResponse));
            }
            catch (Exception ex)
            {
                return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var user = await _userService.GetUserById(id);
                if (user == null)
                    return NotFound(ApiResponse<object>.FailResponse("User not found"));

                var data = new
                {
                    userId = user.Id,
                    username = user.Username,
                    email = user.Email,
                    role = user.Role,
                    createdDate = user.CreatedDate,
                    lastLoginDate = user.LastLoginDate,
                    isActive = user.IsActive
                };

                return Ok(ApiResponse<object>.SuccessResponse("User retrieved successfully", data));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _userService.ForgotPassword(request);
                return Ok(ApiResponse<object>.SuccessResponse("Password reset code sent to your email.", default!));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeRequest request)
        {
            try
            {
                var isValid = _userService.VerifyResetCode(request);
                if (isValid)
                    return Ok(ApiResponse<object>.SuccessResponse("Code verified successfully.", default!));
                
                return BadRequest(ApiResponse<object>.FailResponse("Invalid verification code."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var success = await _userService.ResetPassword(request);
                if (success)
                    return Ok(ApiResponse<object>.SuccessResponse("Password reset successfully.", default!));

                return BadRequest(ApiResponse<object>.FailResponse("Failed to reset password."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // Retrieve user ID claim from authenticated token
                var userId = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<object>.FailResponse("User ID claim is missing."));

                var success = await _userService.ChangePassword(userId, request);
                if (success)
                    return Ok(ApiResponse<object>.SuccessResponse("Password changed successfully.", default!));

                return BadRequest(ApiResponse<object>.FailResponse("Failed to change password."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var success = await _userService.Logout(request.RefreshToken);
                if (success)
                    return Ok(ApiResponse<object>.SuccessResponse("Logged out successfully", default!));
                else
                    return BadRequest(ApiResponse<object>.FailResponse("Logout failed"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }
    }
}