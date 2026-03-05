using NearU_Backend.Server.Models;
using System.Security.Claims;

namespace NearU_Backend.Server.Services.Interfaces;

/// <summary>
/// Service interface for JWT token operations.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a refresh token with device information.
    /// </summary>
    RefreshToken GenerateRefreshToken(string ipAddress, string? userAgent = null, string? deviceInfo = null);

    /// <summary>
    /// Validates a JWT token and returns the principal.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Gets the user ID from a JWT token.
    /// </summary>
    int? GetUserIdFromToken(string token);

    /// <summary>
    /// Gets the role from a JWT token.
    /// </summary>
    string? GetRoleFromToken(string token);
}
