using Microsoft.Extensions.Options;
using NearU_Backend.Server.Configuration;
using NearU_Backend.Server.Models;
using NearU_Backend.Server.Models.DTOs;
using NearU_Backend.Server.Repositories.Interfaces;
using NearU_Backend.Server.Services.Interfaces;

namespace NearU_Backend.Server.Services;

/// <summary>
/// Service for authentication operations with database integration.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ITokenService tokenService,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _tokenService = tokenService;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, string ipAddress, string? userAgent = null, string? deviceInfo = null)
    {
        var user = await ValidateUserCredentialsAsync(request.UsernameOrEmail, request.Password);
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Login failed for user: {UsernameOrEmail}", request.UsernameOrEmail);
            return null;
        }

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(ipAddress, userAgent, deviceInfo);
        refreshToken.UserId = user.Id;

        // Store refresh token
        await _refreshTokenRepository.CreateAsync(refreshToken);

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        return CreateAuthResponse(user, accessToken, refreshToken);
    }

    /// <inheritdoc/>
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, string ipAddress, string? userAgent = null, string? deviceInfo = null)
    {
        // Check if username already exists
        if (await _userRepository.UsernameExistsAsync(request.Username))
        {
            _logger.LogWarning("Registration failed: Username {Username} already exists", request.Username);
            return null;
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email))
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
            return null;
        }

        // Get role
        var role = await _roleRepository.GetByNameAsync(request.Role);
        if (role == null)
        {
            _logger.LogError("Registration failed: Role {Role} not found", request.Role);
            return null;
        }

        // Create new user
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Save user
        user = await _userRepository.CreateAsync(user);

        // Reload user with role
        user = await _userRepository.GetByIdAsync(user.Id) ?? user;

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(ipAddress, userAgent, deviceInfo);
        refreshToken.UserId = user.Id;

        // Store refresh token
        await _refreshTokenRepository.CreateAsync(refreshToken);

        _logger.LogInformation("User {Username} registered successfully with role {Role}", user.Username, request.Role);

        return CreateAuthResponse(user, accessToken, refreshToken);
    }

    /// <inheritdoc/>
    public async Task<AuthResponse?> RefreshTokenAsync(string token, string ipAddress, string? userAgent = null, string? deviceInfo = null)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            _logger.LogWarning("Invalid or expired refresh token");
            return null;
        }

        var user = refreshToken.User;
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User not found or inactive for refresh token");
            return null;
        }

        // Generate new tokens
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken(ipAddress, userAgent, deviceInfo);
        newRefreshToken.UserId = user.Id;

        // Revoke old refresh token
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReplacedByToken = newRefreshToken.Token;
        refreshToken.ReasonRevoked = "Replaced by new token";
        await _refreshTokenRepository.UpdateAsync(refreshToken);

        // Store new refresh token
        await _refreshTokenRepository.CreateAsync(newRefreshToken);

        _logger.LogInformation("Refresh token renewed for user {Username}", user.Username);

        return CreateAuthResponse(user, newAccessToken, newRefreshToken);
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeTokenAsync(string token, string ipAddress)
    {
        return await _refreshTokenRepository.RevokeTokenAsync(token, ipAddress, "Revoked by user");
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeAllUserTokensAsync(int userId, string ipAddress)
    {
        return await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, ipAddress, "Revoked all tokens");
    }

    /// <inheritdoc/>
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    /// <inheritdoc/>
    public async Task<UserResponse?> GetUserInfoAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return null;

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.Name,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin
        };
    }

    /// <inheritdoc/>
    public async Task<User?> ValidateUserCredentialsAsync(string usernameOrEmail, string password)
    {
        var user = await _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail);

        if (user == null)
            return null;

        // Verify password using BCrypt
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return user;
    }

    #region Private Helper Methods

    private AuthResponse CreateAuthResponse(User user, string accessToken, RefreshToken refreshToken)
    {
        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.Name,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            RefreshTokenExpiration = refreshToken.ExpiresAt
        };
    }

    #endregion
}
