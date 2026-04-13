using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.Repositories;
using NearU_Backend_Revised.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on Railway's PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Log configuration for debugging
Console.WriteLine("=== Configuration Debug ===");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
var connStr = builder.Configuration.GetConnectionString("PostgreSQL");
Console.WriteLine($"ConnectionString exists: {!string.IsNullOrEmpty(connStr)}");
if (!string.IsNullOrEmpty(connStr)) Console.WriteLine($"ConnectionString preview: {connStr.Substring(0, Math.Min(50, connStr.Length))}...");
Console.WriteLine($"JWT SecretKey Length: {builder.Configuration["JwtSettings:SecretKey"]?.Length ?? 0}");
Console.WriteLine($"JWT Issuer: {builder.Configuration["JwtSettings:Issuer"]}");
Console.WriteLine("===========================");

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            var response = ApiResponse<object>.FailResponse(string.Join("; ", errors));
            return new BadRequestObjectResult(response);
        };
    });
builder.Services.AddOpenApi();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  return origin.StartsWith("http://localhost") ||
                         origin.StartsWith("https://localhost") ||
                         origin.EndsWith(".up.railway.app") ||
                         origin.EndsWith(".ondigitalocean.app") ||
                         origin == "https://near-u-frontend-pi.vercel.app" ||
                         origin.EndsWith(".vercel.app");
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register JWT Settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (string.IsNullOrEmpty(jwtSettings?.SecretKey))
{
    Console.WriteLine("ERROR: JWT SecretKey is not configured!");
    throw new InvalidOperationException("JWT SecretKey is not configured. Please set JwtSettings__SecretKey environment variable.");
}
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? "")),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
    
    options.AddPolicy("RequireUserId", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("userId");
    });
});

//register imagekit settings
builder.Services.Configure<ImageKitSetting>(
    builder.Configuration.GetSection("ImageKit")
);

// Food feature
builder.Services.AddScoped<IFoodShopRepository, FoodShopRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IFoodShopService, FoodShopService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<IImageService, ImageService>();

// Accommodation feature
builder.Services.AddScoped<IAccommodationRepository, AccommodationRepository>();
builder.Services.AddScoped<IAccommodationItemRepository, AccommodationItemRepository>();
builder.Services.AddScoped<IAccommodationService, AccommodationService>();
builder.Services.AddScoped<IAccommodationItemService, AccommodationItemService>();

// Configure Database (PostgreSQL only)
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: PostgreSQL connection string is not configured!");
    throw new InvalidOperationException("PostgreSQL connection string is not configured. Please set ConnectionStrings__PostgreSQL environment variable.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });
});

// Register repositories and services
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();


var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
