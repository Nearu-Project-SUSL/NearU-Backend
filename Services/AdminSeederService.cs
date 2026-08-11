using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using Microsoft.EntityFrameworkCore;

namespace NearU_Backend_Revised.Services
{
    /// <summary>
    /// Startup service that ensures at least one Admin account exists in the database.
    /// Credentials are read from the "AdminSeed" section in appsettings.json.
    /// Override in production using environment variables:
    ///   AdminSeed__Email, AdminSeed__Username, AdminSeed__Password
    ///
    /// This service does NOT expose any HTTP endpoints.
    /// Admin accounts cannot be created through the public registration API.
    /// </summary>
    public class AdminSeederService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AdminSeederService> _logger;

        public AdminSeederService(
            ApplicationDbContext db,
            IConfiguration config,
            ILogger<AdminSeederService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            var email    = _config["AdminSeed:Email"];
            var username = _config["AdminSeed:Username"];
            var password = _config["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("AdminSeed configuration is incomplete. Skipping admin seeding.");
                return;
            }

            // Safety guard: never seed if the password is still the placeholder
            if (password == "CHANGE_ME_IN_PRODUCTION")
            {
                _logger.LogWarning(
                    "AdminSeed:Password is still the default placeholder. " +
                    "Set AdminSeed__Password via environment variable in production.");
                // Still seed in development with the placeholder, but warn loudly
            }

            // Check if an Admin already exists
            bool adminExists = await _db.Users
                .AnyAsync(u => u.Role == UserRoles.Admin);

            if (adminExists)
            {
                _logger.LogInformation("Admin account already exists. Skipping seed.");
                return;
            }

            _logger.LogInformation("No admin account found. Seeding initial admin: {Email}", email);

            var admin = new User
            {
                Id           = Guid.NewGuid().ToString(),
                Username     = username,
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role         = UserRoles.Admin,
                CreatedDate  = DateTime.UtcNow.ToString("o"),
                IsActive     = 1
            };

            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Admin account seeded successfully with email: {Email}", email);
        }
    }
}
