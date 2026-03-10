using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NearU_Backend.Server.Models;
using NearU_Backend.Server.Models.DTOs;
using NearU_Backend.Server.Services.Interfaces;

namespace NearU_Backend.Server.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();
        var deviceInfo = GetDeviceInfo();

        var response = await _authService.RegisterAsync(request, ipAddress, userAgent, deviceInfo);

        if (response == null)
        {
            return BadRequest(new { message = "Username or email already exists" });
        }

        SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiration);

        _logger.LogInformation("User {Username} registered successfully", request.Username);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();
        var deviceInfo = GetDeviceInfo();

        var response = await _authService.LoginAsync(request, ipAddress, userAgent, deviceInfo);

        if (response == null)
        {
            return Unauthorized(new { message = "Invalid username/email or password" });
        }

        SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiration);

        _logger.LogInformation("User {Username} logged in successfully", response.Username);
        return Ok(response);
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">Refresh token request (optional if token is in cookie)</param>
    /// <returns>New authentication response with tokens</returns>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request = null)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();
        var deviceInfo = GetDeviceInfo();

        var response = await _authService.RefreshTokenAsync(refreshToken, ipAddress, userAgent, deviceInfo);

        if (response == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiration);

        return Ok(response);
    }

    /// <summary>
    /// Revokes a refresh token (logout).
    /// </summary>
    /// <param name="request">Refresh token to revoke (optional if token is in cookie)</param>
    /// <returns>Success message</returns>
    [HttpPost("revoke-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest? request = null)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        var ipAddress = GetIpAddress();
        var result = await _authService.RevokeTokenAsync(refreshToken, ipAddress);

        if (!result)
        {
            return BadRequest(new { message = "Invalid refresh token" });
        }

        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Token revoked successfully" });
    }

    /// <summary>
    /// Revokes all refresh tokens for the current user (logout from all devices).
    /// </summary>
    /// <returns>Success message</returns>
    [Authorize]
    [HttpPost("revoke-all-tokens")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllTokens()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = GetIpAddress();
        await _authService.RevokeAllUserTokensAsync(userId.Value, ipAddress);

        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "All tokens revoked successfully" });
    }

    /// <summary>
    /// Gets the current authenticated user's information.
    /// </summary>
    /// <returns>User information</returns>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var userInfo = await _authService.GetUserInfoAsync(userId.Value);
        if (userInfo == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(userInfo);
    }

    /// <summary>
    /// Admin-only endpoint example.
    /// </summary>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("admin-only")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "This is an admin-only endpoint", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Student-only endpoint example.
    /// </summary>
    [Authorize(Roles = UserRoles.Student)]
    [HttpGet("student-only")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult StudentOnly()
    {
        return Ok(new { message = "This is a student-only endpoint", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Business-only endpoint example.
    /// </summary>
    [Authorize(Roles = UserRoles.Business)]
    [HttpGet("business-only")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult BusinessOnly()
    {
        return Ok(new { message = "This is a business-only endpoint", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Rider-only endpoint example.
    /// </summary>
    [Authorize(Roles = UserRoles.Rider)]
    [HttpGet("rider-only")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult RiderOnly()
    {
        return Ok(new { message = "This is a rider-only endpoint", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Multiple roles endpoint example (Student or Business).
    /// </summary>
    [Authorize(Roles = $"{UserRoles.Student},{UserRoles.Business}")]
    [HttpGet("student-or-business")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult StudentOrBusiness()
    {
        return Ok(new { message = "This is accessible by students and businesses", timestamp = DateTime.UtcNow });
    }

    #region Private Helper Methods

    private void SetRefreshTokenCookie(string refreshToken, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expires
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string? GetUserAgent()
    {
        return Request.Headers.UserAgent.ToString();
    }

    private string? GetDeviceInfo()
    {
        var userAgent = GetUserAgent();
        if (string.IsNullOrEmpty(userAgent))
            return null;

        // Basic device detection (can be enhanced with a library like UAParser)
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
            return "Mobile";
        if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            return "Tablet";
        return "Desktop";
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    #endregion
}
