using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using NearU_Backend_Revised.DTOs.Auth;
using NearU_Backend_Revised.DTOs.User;
using NearU_Backend_Revised.Repositories;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.Configuration;
using BCrypt.Net;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Enums;
using Microsoft.AspNetCore.SignalR;
using NearU_Backend_Revised.Hubs;
using Microsoft.EntityFrameworkCore;

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
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<RidesHub> _hubContext;
        private readonly IConfiguration _configuration;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Code, DateTime Expiry)> _resetCodes = new();

        public UserService(
            UserRepository userrepo, 
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepo,
            IOptions<JwtSettings> jwtSettings,
            IImageService imageService,
            IEmailService emailService,
            ApplicationDbContext dbContext,
            IHubContext<RidesHub> hubContext,
            IConfiguration configuration)
        {
            _userRepo = userrepo;
            _tokenService = tokenService;
            _refreshTokenRepo = refreshTokenRepo;
            _jwtSettings = jwtSettings.Value;
            _imageService = imageService;
            _emailService = emailService;
            _dbContext = dbContext;
            _hubContext = hubContext;
            _configuration = configuration;
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

            // FIX: Initialize RiderStatus immediately upon registration
            if (user.Role == "Rider")
            {
                var riderStatus = new RiderStatus
                {
                    RiderId = user.Id,
                    IsOnline = false,
                    ApprovalStatus = RiderApprovalStatus.Pending,
                    LastSeen = DateTime.UtcNow
                };
                _dbContext.RiderStatuses.Add(riderStatus);
                await _dbContext.SaveChangesAsync();

                // Method A: Email Notification to Admins via SendGrid
                try
                {
                    var adminEmail = _configuration["AdminSeed:Email"] ?? "admin@nearusab.me";
                    var subject = "New Rider Application - Review Required 🛵";
                    var plainText = $"Rider {user.Username} ({user.Email}) has registered and is pending approval.";
                    var htmlContent = $@"
<div style=""font-family: 'Inter', sans-serif; background-color: #0d0e12; color: #ffffff; padding: 40px 20px; text-align: center; border-radius: 8px; max-width: 600px; margin: 0 auto; border: 1px solid rgba(255,255,255,0.1);"">
    <div style=""margin-bottom: 20px;"">
        <h1 style=""color: #2E9EBF; margin: 0; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;"">NearU</h1>
        <p style=""color: #9ca3af; font-size: 14px; margin-top: 5px;"">Rider Registration Center</p>
    </div>
    <div style=""background: rgba(255, 255, 255, 0.03); border: 1px solid rgba(255, 255, 255, 0.08); border-radius: 12px; padding: 30px; margin-bottom: 20px; box-shadow: 0 4px 30px rgba(0, 0, 0, 0.5); backdrop-filter: blur(10px); text-align: left;"">
        <h2 style=""color: #f3f4f6; margin-top: 0; font-size: 20px; font-weight: 600; text-align: center;"">New Rider Application Received</h2>
        <p style=""color: #9ca3af; font-size: 16px; line-height: 1.6; margin-bottom: 25px;"">A new rider has successfully registered on NearU and is currently pending review. Please log in to the admin console to approve or reject this applicant.</p>
        
        <table style=""width: 100%; color: #9ca3af; border-collapse: collapse; margin-bottom: 25px;"">
            <tr>
                <td style=""padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.05); font-weight: bold;"">Rider Name:</td>
                <td style=""padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.05); color: #ffffff;"">{System.Net.WebUtility.HtmlEncode(user.Username)}</td>
            </tr>
            <tr>
                <td style=""padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.05); font-weight: bold;"">Email:</td>
                <td style=""padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.05); color: #ffffff;"">{System.Net.WebUtility.HtmlEncode(user.Email)}</td>
            </tr>
            <tr>
                <td style=""padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.05); font-weight: bold;"">Phone:</td>
                <td style=""padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.05); color: #ffffff;"">{System.Net.WebUtility.HtmlEncode(user.MobileNumber ?? "N/A")}</td>
            </tr>
            <tr>
                <td style=""padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.05); font-weight: bold;"">Address:</td>
                <td style=""padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.05); color: #ffffff;"">{System.Net.WebUtility.HtmlEncode(user.Address ?? "N/A")}</td>
            </tr>
        </table>
        
        <div style=""text-align: center;"">
            <a href=""https://admin.nearusab.me/riders"" style=""background: linear-gradient(135deg, #2E9EBF 0%, #1a7a9a 100%); padding: 15px 30px; border-radius: 8px; display: inline-block; font-size: 16px; font-weight: 700; color: #ffffff; text-decoration: none; box-shadow: 0 4px 15px rgba(46, 158, 191, 0.4);"">Review & Approve Rider</a>
        </div>
    </div>
    <div style=""color: #4b5563; font-size: 12px; margin-top: 30px; border-top: 1px solid rgba(255,255,255,0.05); padding-top: 20px;"">
        &copy; 2026 NearU Inc. All rights reserved.
    </div>
</div>";

                    await _emailService.SendEmailAsync(adminEmail, subject, plainText, htmlContent);
                }
                catch (Exception ex)
                {
                    // Catch exception to make sure failing email doesn't crash rider registration.
                    Console.WriteLine($"[Email Service Error] Failed to send admin email: {ex.Message}");
                }

                // Method B: SignalR Broadcast to Admin Console
                try
                {
                    await _hubContext.Clients.Group("Admins").SendAsync("NewRiderApplication", new
                    {
                        riderId = user.Id,
                        name = user.Username,
                        email = user.Email,
                        mobileNumber = user.MobileNumber,
                        registeredAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    // Catch exception to prevent SignalR disconnects from crashing registration.
                    Console.WriteLine($"[SignalR Error] Failed to broadcast admin alert: {ex.Message}");
                }
            }

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

        public async Task<bool> DeleteAccountAsync(string userId, string password)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new Exception("Incorrect password.");

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Delete user posted jobs to avoid Restrict constraint violation
                var userJobs = _dbContext.Jobs.Where(j => j.PostedByUserId == userId);
                _dbContext.Jobs.RemoveRange(userJobs);

                // 2. Delete user ride requests as a student to avoid Restrict constraint violation
                var studentRides = _dbContext.RideRequests.Where(r => r.StudentId == userId);
                _dbContext.RideRequests.RemoveRange(studentRides);

                // 3. For ride requests as a rider, set RiderId to null
                var riderRides = _dbContext.RideRequests.Where(r => r.RiderId == userId);
                foreach (var ride in riderRides)
                {
                    ride.RiderId = null;
                }

                // 4. Remove RiderStatus if the user is a rider
                var riderStatus = await _dbContext.RiderStatuses.FindAsync(userId);
                if (riderStatus != null)
                {
                    _dbContext.RiderStatuses.Remove(riderStatus);
                }

                // 5. Remove FCM tokens
                var fcmTokens = _dbContext.UserFcmTokens.Where(t => t.UserId == userId);
                _dbContext.UserFcmTokens.RemoveRange(fcmTokens);

                // 6. Remove refresh tokens
                var refreshTokens = _dbContext.RefreshTokens.Where(rt => rt.UserId == userId);
                _dbContext.RefreshTokens.RemoveRange(refreshTokens);

                // 7. Finally, remove the user
                _dbContext.Users.Remove(user);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}



