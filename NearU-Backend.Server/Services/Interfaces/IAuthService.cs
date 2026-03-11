using NearU_Backend.Server.Models;
using NearU_Backend.Server.Models.DTOs;

namespace NearU_Backend.Server.Services.Interfaces;

/// <summary>
/// Service interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and returns authentication tokens.
    /// </summary>
    Task<AuthResponse?> LoginAsync(LoginRequest request, string ipAddress, string? userAgent = null, string? deviceInfo = null);

    /// <summary>
    /// Registers a new user.
    /// </summary>
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, string ipAddress, string? userAgent = null, string? deviceInfo = null);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string ipAddress, string? userAgent = null, string? deviceInfo = null);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    Task<bool> RevokeTokenAsync(string refreshToken, string ipAddress);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// </summary>
    Task<bool> RevokeAllUserTokensAsync(int userId, string ipAddress);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<User?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Gets user information response.
    /// </summary>
    Task<UserResponse?> GetUserInfoAsync(int userId);

    /// <summary>
    /// Validates user credentials.
    /// </summary>
    Task<User?> ValidateUserCredentialsAsync(string usernameOrEmail, string password);
}
