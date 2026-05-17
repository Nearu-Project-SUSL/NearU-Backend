using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NearU_Backend_Revised.DTOs.Auth;
using NearU_Backend_Revised.DTOs.User;
using NearU_Backend_Revised.Repositories;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;
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
        private readonly IImageService _imageService;

        public UserService(
            UserRepository userrepo, 
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepo,
            IOptions<JwtSettings> jwtSettings,
            IImageService imageService)
        {
            _userRepo = userrepo;
            _tokenService = tokenService;
            _refreshTokenRepo = refreshTokenRepo;
            _jwtSettings = jwtSettings.Value;
            _imageService = imageService;
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

        public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request)
        {
            // Verify access token with Google
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/userinfo?access_token={request.Token}");
            
            if (!response.IsSuccessStatusCode)
                throw new Exception("Invalid Google token");

            var payload = await System.Text.Json.JsonSerializer.DeserializeAsync<GoogleUserInfoPayload>(
                await response.Content.ReadAsStreamAsync()
            );

            if (payload == null || string.IsNullOrEmpty(payload.Email))
                throw new Exception("Failed to retrieve user information from Google");

            var user = await _userRepo.GetUserByEmail(payload.Email);
            
            if (user == null)
            {
                // Create user if not exists
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = payload.Name ?? payload.Email.Split('@')[0],
                    Email = payload.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password
                    Role = "Student", // Default role
                    CreatedDate = DateTime.UtcNow.ToString("o"),
                    IsActive = 1
                };
                await _userRepo.AddUser(user);
            }

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

        public async Task<User> UpdateProfileAsync(string userId, UpdateProfileRequest request)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            if (!string.IsNullOrEmpty(request.Username)) user.Username = request.Username;
            if (!string.IsNullOrEmpty(request.MobileNumber)) user.MobileNumber = request.MobileNumber;
            if (!string.IsNullOrEmpty(request.Faculty)) user.Faculty = request.Faculty;
            if (!string.IsNullOrEmpty(request.Year)) user.Year = request.Year;
            if (!string.IsNullOrEmpty(request.Address)) user.Address = request.Address;
            if (!string.IsNullOrEmpty(request.City)) user.City = request.City;
            if (!string.IsNullOrEmpty(request.DateOfBirth)) user.DateOfBirth = request.DateOfBirth;

            await _userRepo.UpdateUserAsync(user);
            return user;
        }

        public async Task<User> UpdateProfilePictureAsync(string userId, IFormFile file)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            if (file == null || file.Length == 0)
                throw new Exception("File is empty");

            // Upload image using ImageService
            var folder = "user_profiles";
            var imageUrl = await _imageService.UploadImageAsync(file, folder);

            user.ProfilePictureUrl = imageUrl;
            await _userRepo.UpdateUserAsync(user);

            return user;
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



