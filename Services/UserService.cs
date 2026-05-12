using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.DTOs.Auth;
using NearU_Backend_Revised.Repositories;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Configuration;
using BCrypt.Net;

namespace NearU_Backend_Revised.Services
{
    public class UserService
    {
        private readonly UserRepository _userRepo;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly JwtSettings _jwtSettings;

        public UserService(
            UserRepository userrepo, 
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepo,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepo = userrepo;
            _tokenService = tokenService;
            _refreshTokenRepo = refreshTokenRepo;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<User> Register(RegisterRequest request)
        {
            var existingUser = await _userRepo.GetUserByEmail(request.Email);
            if (existingUser != null) throw new Exception("User already exists");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = request.Username,
                Email = request.Email,
                MobileNumber = request.MobileNumber,
                StudentId = request.StudentId,
                Faculty = request.Faculty,
                Year = request.Year,
                Address = request.Address,
                City = request.City,
                DateOfBirth = request.DateOfBirth,
                PasswordHash = hashedPassword,
                Role = "User",
                CreatedDate = DateTime.UtcNow.ToString("o"),
                IsActive = 1
            };

            await _userRepo.AddUser(user);
            return user;
        }

        /// <summary>
        /// Login user and return authentication response with tokens
        /// </summary>
        public async Task<AuthResponse> Login(LoginRequest request)
        {
            var user = await _userRepo.GetUserByEmail(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");

            // Generate access token
            var accessToken = _tokenService.GenerateAccessToken(user);

            // Generate refresh token
            var refreshToken = _tokenService.GenerateRefreshToken(user.Id);
            await _refreshTokenRepo.SaveRefreshTokenAsync(refreshToken);

            // Build response
            return new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryInMinutes),
                RefreshTokenExpiry = refreshToken.ExpiryDate
            };
        }

        /// <summary>
        /// Refresh access token using valid refresh token
        /// </summary>
        public async Task<AuthResponse> RefreshToken(RefreshTokenRequest request)
        {
            // Rotate refresh token (validates old token and creates new one)
            var newRefreshToken = await _tokenService.RotateRefreshToken(request.RefreshToken);
            if (newRefreshToken == null)
                throw new Exception("Invalid or expired refresh token");

            // Get user to generate new access token
            var user = await _userRepo.GetByIdAsync(newRefreshToken.UserId);
            if (user == null)
                throw new Exception("User not found");

            // Generate new access token
            var accessToken = _tokenService.GenerateAccessToken(user);

            // Build response
            return new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryInMinutes),
                RefreshTokenExpiry = newRefreshToken.ExpiryDate
            };
        }

        public async Task<User?> GetUserById(string id)
        {
            return await _userRepo.GetByIdAsync(id);
        }

        /// <summary>
        /// Logout user by revoking their refresh token
        /// </summary>
        public async Task<bool> Logout(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                throw new ArgumentException("Refresh token is required");

            return await _refreshTokenRepo.RevokeRefreshTokenAsync(refreshToken, "User logout");
        }

        /// <summary>
        /// Logout user from all devices by revoking all refresh tokens
        /// </summary>
        public async Task<int> LogoutAllDevices(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID is required");

            return await _refreshTokenRepo.RevokeAllUserTokensAsync(userId, "Logout all devices");
        }
    }
}



