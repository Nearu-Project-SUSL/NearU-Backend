using Microsoft.AspNetCore.Mvc;
using NearU_Backend_Revised.Services;
using NearU_Backend_Revised.DTOs.Auth;
using NearU_Backend_Revised.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.Configuration;


namespace NearU_Backend_Revised.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserService userService,
            ITokenService tokenService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
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
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("Invalid role", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("required for business", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("BusinessType must be", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("cannot be created via registration", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during registration for email={Email}", request.Email);
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("login-limit")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var authResponse = await _userService.Login(request);
                return Ok(ApiResponse<object>.SuccessResponse("Login successful", authResponse));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex) when (ex.Message.Equals("Invalid credentials", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("deactivated", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("suspended", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during login for email={Email}", request.Email);
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
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
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid Google token", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("deactivated", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during Google login");
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
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
            catch (Exception ex) when (ex.Message.Contains("Invalid or expired refresh token", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("User not found", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during token refresh");
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
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
                _logger.LogError(ex, "Unhandled error fetching user id={UserId}", id);
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
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
            catch (Exception ex) when (ex.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            {
                // Return generic message to avoid user-enumeration attacks
                _logger.LogInformation("ForgotPassword requested for unknown email={Email}", request.Email);
                return Ok(ApiResponse<object>.SuccessResponse("If that email is registered, a reset code has been sent.", default!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during forgot-password for email={Email}", request.Email);
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
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
            catch (Exception ex) when (ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("Invalid verification code", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("No verification code", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during verify-reset-code");
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
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
            catch (Exception ex) when (ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("Invalid verification code", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("No verification code", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("User not found", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during password reset");
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<object>.FailResponse("User ID claim is missing."));

                var success = await _userService.ChangePassword(userId, request);
                if (success)
                    return Ok(ApiResponse<object>.SuccessResponse("Password changed successfully.", default!));

                return BadRequest(ApiResponse<object>.FailResponse("Failed to change password."));
            }
            catch (Exception ex) when (ex.Message.Contains("incorrect", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("wrong password", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during change-password");
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
            }
        }

        /// <summary>
        /// Standard logout — revokes the refresh token in DB and blacklists the current device's jti.
        /// Idempotent: returns 200 even if the refresh token was already revoked.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var jti    = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                var remaining = TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpiryInMinutes);

                // 1. Revoke the refresh token in the database (best-effort — already-revoked is fine)
                var revoked = await _userService.Logout(request.RefreshToken);
                if (!revoked)
                    _logger.LogInformation("Logout: refresh token was already revoked or not found (userId={UserId}).", userId);

                // 2. Blacklist the current device's access token in Redis
                if (!string.IsNullOrWhiteSpace(jti))
                    await _tokenService.BlacklistTokenAsync(jti, remaining);

                // 3. Remove this jti from the user's active-token Set (FIX: was incorrectly calling TrackActiveTokenAsync which ADDS)
                if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(jti))
                    await _tokenService.RemoveActiveTokenAsync(userId, jti);

                return Ok(ApiResponse<object>.SuccessResponse("Logged out successfully", default!));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during logout");
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
            }
        }

        /// <summary>
        /// Sign-Out-All-Devices — blacklists every active access token for the authenticated user
        /// and revokes all their refresh tokens in the DB.
        ///
        /// Mechanism:
        ///   1. Reads the per-user active-JTI Redis Set built up by GenerateAccessToken.
        ///   2. Adds each jti to the blacklist (checked by TokenBlacklistMiddleware).
        ///   3. Deletes the JTI Set.
        ///   4. Calls UserService to revoke all DB refresh tokens for the user.
        /// </summary>
        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(ApiResponse<object>.FailResponse("User ID claim is missing."));

                var tokenLifetime = TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpiryInMinutes);

                // Blacklist all tracked access tokens and wipe the JTI Set
                await _tokenService.BlacklistAllUserTokensAsync(userId, tokenLifetime);

                // Revoke all refresh tokens in DB
                var revokedCount = await _userService.LogoutAllDevices(userId);
                _logger.LogInformation("LogoutAll: revoked {Count} refresh token(s) for userId={UserId}.", revokedCount, userId);

                return Ok(ApiResponse<object>.SuccessResponse(
                    "Signed out from all devices successfully.", default!));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during logout-all");
                return StatusCode(500, ApiResponse<object>.FailResponse("An unexpected error occurred. Please try again."));
            }
        }
    }
}