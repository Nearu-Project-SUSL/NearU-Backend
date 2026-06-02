using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NearU_Backend_Revised.BackgroundServices;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using NearU_Backend_Revised.Hubs;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services;
using NearU_Backend_Revised.Services.Interfaces;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

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

// Health checks — used by the Docker Compose healthcheck directive
builder.Services.AddHealthChecks();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  return origin.StartsWith("http://localhost") ||
                         origin.StartsWith("https://localhost") ||
                         origin == "https://near-u-frontend-pi.vercel.app" ||
                         origin.EndsWith(".vercel.app") ||
                         origin == "https://nearusab.me" ||
                         origin == "https://www.nearusab.me" ||
                         origin == "https://api.nearusab.me";
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Rate Limiting for Login
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login-limit", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(15);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });
});

// Register JWT Settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (string.IsNullOrEmpty(jwtSettings?.SecretKey))
    throw new InvalidOperationException("JWT SecretKey is not configured. Set JwtSettings:SecretKey in appsettings or the JwtSettings__SecretKey environment variable.");
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
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? "")
        ),
        ClockSkew = TimeSpan.FromMinutes(5),

        RoleClaimType = ClaimTypes.Role,       
        NameClaimType = ClaimTypes.NameIdentifier  
    };

    // SignalR WebSocket connections cannot set HTTP headers, so clients pass the
    // JWT as ?access_token=... in the query string. This reads it transparently.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && 
                path.StartsWithSegments("/hubs/rides"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };

});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("RequireUserId", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("userId");
    });

    // ── Role-based policies ──────────────────────────────────────────────────
    options.AddPolicy("RequireStudent", policy =>
        policy.RequireAuthenticatedUser().RequireRole(UserRoles.Student));

    options.AddPolicy("RequireRider", policy =>
        policy.RequireAuthenticatedUser().RequireRole(UserRoles.Rider));

    options.AddPolicy("RequireBusiness", policy =>
        policy.RequireAuthenticatedUser().RequireRole(UserRoles.Business));

    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireAuthenticatedUser().RequireRole(UserRoles.Admin));

    // Business owners and admins can both manage listings
    options.AddPolicy("RequireBusinessOrAdmin", policy =>
        policy.RequireAuthenticatedUser().RequireRole(UserRoles.Business, UserRoles.Admin));
});

builder.Services.Configure<ImageKitSettings>(
    builder.Configuration.GetSection("ImageKit"));

// Food feature
builder.Services.AddScoped<IFoodShopRepository, FoodShopRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IFoodShopService, FoodShopService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IImageService, ImageService>();

//testimonial
builder.Services.AddScoped<ITestimonialRepository, TestimonialRepository>();
builder.Services.AddScoped<ITestimonialService, TestimonialService>();


// Accommodation feature
builder.Services.AddScoped<IAccommodationRepository, AccommodationRepository>();
builder.Services.AddScoped<IAccommodationItemRepository, AccommodationItemRepository>();
builder.Services.AddScoped<IAccommodationService, AccommodationService>();
builder.Services.AddScoped<IAccommodationItemService, AccommodationItemService>();


// Gift feature
builder.Services.AddScoped<IGiftShopRepository, GiftShopRepository>();
builder.Services.AddScoped<IGiftShopService, GiftShopService>();

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("PostgreSQL connection string is not configured. Set ConnectionStrings:PostgreSQL in appsettings or the ConnectionStrings__PostgreSQL environment variable.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseNetTopologySuite();
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        );
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.UseNetTopologySuite(); // map Point type to PostGIS geography
    });
});

// Register repositories and services
builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("SendGrid"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<AdminSeederService>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();

builder.Services.Configure<RideSettings>(
    builder.Configuration.GetSection("RideSettings"));

// ── OSRM routing service ─────────────────────────────────────────────────────
builder.Services.Configure<OsrmSettings>(
    builder.Configuration.GetSection("OsrmSettings"));
// AddHttpClient<T> creates a typed HttpClient scoped to OsrmService.
// Retry/timeout configuration lives in OsrmSettings; the HttpClient here is
// intentionally vanilla so OsrmService can set BaseAddress/Timeout itself.
builder.Services.AddHttpClient<IOsrmService, OsrmService>();
// ─────────────────────────────────────────────────────────────────────────────

builder.Services.AddScoped<IRideRepository, RideRepository>();
builder.Services.AddScoped<IRideService, RideService>();
builder.Services.AddScoped<IRideStateMachine, RideStateMachine>();
builder.Services.AddScoped<IRideNotificationService, RideNotificationService>();
builder.Services.AddScoped<IFcmTokenService, FcmTokenService>();
builder.Services.AddHostedService<GhostRiderCleanupWorker>();
builder.Services.AddHostedService<RideLifecycleWorker>();

// Redis Integration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "NearU_";
    });
    
    // Add SignalR with Redis Backplane
    builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString, options => 
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("NearU_SignalR");
    });
}
else
{
    // Fallback to in-memory cache if Redis is not configured
    builder.Services.AddDistributedMemoryCache();
    
    // Fallback to standard SignalR without backplane
    builder.Services.AddSignalR();
}

// Firebase Admin Setup
var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"];
if (!string.IsNullOrEmpty(firebaseCredentialsPath) && System.IO.File.Exists(firebaseCredentialsPath))
{
#pragma warning disable CS0618
    using (var stream = new System.IO.FileStream(firebaseCredentialsPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
    {
        FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
        {
            Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(stream)
        });
    }
#pragma warning restore CS0618
}




var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Database migration failed (possibly due to missing PostGIS extension locally). Continuing with fallback table creation...");
        }

        // Ensure GiftShop tables exist in case EF Migrations History is out of sync
        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""GiftShops"" (
                ""Id"" uuid NOT NULL,
                ""Name"" character varying(150) NOT NULL,
                ""ImageUrl"" character varying(500),
                ""LocationName"" character varying(150) NOT NULL,
                ""Phone"" character varying(20) NOT NULL,
                ""Email"" character varying(150),
                ""Address"" character varying(500) NOT NULL,
                ""IsActive"" boolean NOT NULL DEFAULT TRUE,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_GiftShops"" PRIMARY KEY (""Id"")
            );

            CREATE TABLE IF NOT EXISTS ""GiftProducts"" (
                ""Id"" uuid NOT NULL,
                ""GiftShopId"" uuid NOT NULL,
                ""Name"" character varying(150) NOT NULL,
                ""PhotoUrl"" character varying(500),
                ""Price"" numeric(18,2) NOT NULL,
                ""IsActive"" boolean NOT NULL DEFAULT TRUE,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_GiftProducts"" PRIMARY KEY (""Id""),
                CONSTRAINT ""FK_GiftProducts_GiftShops_GiftShopId"" FOREIGN KEY (""GiftShopId"") REFERENCES ""GiftShops"" (""Id"") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ""IX_GiftProducts_GiftShopId"" ON ""GiftProducts"" (""GiftShopId"");
        ");

        // Seed the initial Admin account from configuration
        var seeder = services.GetRequiredService<AdminSeederService>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "FATAL: Database initialization fallback failed!");
        throw;
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

// Trust X-Forwarded-For and X-Forwarded-Proto from Nginx reverse proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRouting();

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RidesHub>("/hubs/rides", options =>
{
    // Enable stateful reconnects to handle clients losing connection temporarily
    options.AllowStatefulReconnects = true;
});

// Health check endpoint — polled by Docker every 30 seconds
app.MapHealthChecks("/healthz");

app.Run();
