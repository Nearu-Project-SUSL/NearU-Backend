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
        private readonly IEmailService _emailService;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Code, DateTime Expiry)> _resetCodes = new();

        public UserService(
            UserRepository userrepo, 
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepo,
            IOptions<JwtSettings> jwtSettings,
            IImageService imageService,
            IEmailService emailService)
        {
            _userRepo = userrepo;
            _tokenService = tokenService;
            _refreshTokenRepo = refreshTokenRepo;
            _jwtSettings = jwtSettings.Value;
            _imageService = imageService;
            _emailService = emailService;
        }

        public async Task<User> Register(RegisterRequest request)
        {
            var existingUser = await _userRepo.GetUserByEmail(request.Email);
            if (existingUser != null) throw new Exception("User already exists");

            // Service-layer guard: Admin accounts cannot be created via self-registration.
            // The DTO regex already blocks this, but we enforce it here too for defense-in-depth.
            if (string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Admin accounts cannot be created via registration. Contact your system administrator.");

            if (!UserRoles.IsValidRole(request.Role))
                throw new Exception($"Invalid role '{request.Role}'. Allowed roles: Student, Rider, Business.");

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
                Role = request.Role,
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

        public async Task ForgotPassword(ForgotPasswordRequest request)
        {
            var user = await _userRepo.GetUserByEmail(request.Email);
            if (user == null)
                throw new Exception("User with this email does not exist.");

            // Generate a secure 6-digit random code
            var code = Random.Shared.Next(100000, 999999).ToString();
            
            // Code expires in 15 minutes
            _resetCodes[request.Email] = (code, DateTime.UtcNow.AddMinutes(15));
            Console.WriteLine($"[TESTING ONLY] ForgotPassword verification code for {request.Email}: {code}");

            var plainText = $"Your NearU password reset verification code is: {code}. This code is valid for 15 minutes.";
            var html = $@"
<div style=""font-family: 'Inter', sans-serif; background-color: #0d0e12; color: #ffffff; padding: 40px 20px; text-align: center; border-radius: 8px; max-width: 600px; margin: 0 auto; border: 1px solid rgba(255,255,255,0.1);"">
    <div style=""margin-bottom: 20px;"">
        <h1 style=""color: #6366f1; margin: 0; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;"">NearU</h1>
        <p style=""color: #9ca3af; font-size: 14px; margin-top: 5px;"">Your Sabaragamuwa University Companion</p>
    </div>
    <div style=""background: rgba(255, 255, 255, 0.03); border: 1px solid rgba(255, 255, 255, 0.08); border-radius: 12px; padding: 30px; margin-bottom: 20px; box-shadow: 0 4px 30px rgba(0, 0, 0, 0.5); backdrop-filter: blur(10px);"">
        <h2 style=""color: #f3f4f6; margin-top: 0; font-size: 20px; font-weight: 600;"">Password Reset Verification Code</h2>
        <p style=""color: #9ca3af; font-size: 16px; line-height: 1.6; margin-bottom: 25px;"">You have requested to reset your password. Use the verification code below to proceed. This code is valid for 15 minutes.</p>
        <div style=""background: linear-gradient(135deg, #6366f1 0%, #a855f7 100%); padding: 15px 30px; border-radius: 8px; display: inline-block; letter-spacing: 6px; font-size: 32px; font-weight: 700; color: #ffffff; margin-bottom: 25px; box-shadow: 0 4px 15px rgba(99, 102, 241, 0.4); text-align: center;"">{code}</div>
        <p style=""color: #6b7280; font-size: 13px; margin: 0;"">If you didn't request this reset, you can safely ignore this email.</p>
    </div>
    <div style=""color: #4b5563; font-size: 12px; margin-top: 30px; border-top: 1px solid rgba(255,255,255,0.05); padding-top: 20px;"">
        &copy; 2026 NearU Inc. All rights reserved.
    </div>
</div>";

            await _emailService.SendEmailAsync(request.Email, "Reset Your NearU Password", plainText, html);
        }

        public bool VerifyResetCode(VerifyResetCodeRequest request)
        {
            if (!_resetCodes.TryGetValue(request.Email, out var resetInfo))
                throw new Exception("No verification code requested for this email.");

            if (resetInfo.Expiry < DateTime.UtcNow)
            {
                _resetCodes.TryRemove(request.Email, out _);
                throw new Exception("Verification code has expired. Please request a new one.");
            }

            if (resetInfo.Code != request.Code)
                throw new Exception("Invalid verification code.");

            return true;
        }

        public async Task<bool> ResetPassword(ResetPasswordRequest request)
        {
            if (!_resetCodes.TryGetValue(request.Email, out var resetInfo))
                throw new Exception("No verification code found for this email.");

            if (resetInfo.Expiry < DateTime.UtcNow)
            {
                _resetCodes.TryRemove(request.Email, out _);
                throw new Exception("Verification code has expired. Please request a new one.");
            }

            if (resetInfo.Code != request.Code)
                throw new Exception("Invalid verification code.");

            var user = await _userRepo.GetUserByEmail(request.Email);
            if (user == null)
                throw new Exception("User not found.");

            // Update user password using BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepo.UpdateUserAsync(user);

            // Clean up code
            _resetCodes.TryRemove(request.Email, out _);
            return true;
        }

        public async Task<bool> ChangePassword(string userId, ChangePasswordRequest request)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                throw new Exception("Incorrect current password.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepo.UpdateUserAsync(user);
            return true;
        }
    }
}



