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
using NearU_Backend_Revised.Middleware;
using AspNetCoreRateLimit;
using System.Security.Claims;
using Scalar.AspNetCore;

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

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "NearU API";
        document.Info.Version = "v1";
        document.Info.Description = "The core backend RESTful API powering the NearU platform: A University Lifestyle Hub and Local Business Marketplace.";
        
        // Add JWT Bearer Security Scheme
        var securityScheme = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Name = "Authorization",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
        };
        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes!.Add("Bearer", securityScheme);
        
        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any())
        {
            operation.Security = new List<Microsoft.OpenApi.OpenApiSecurityRequirement>
            {
                new()
                {
                    [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
                }
            };
        }
        return Task.CompletedTask;
    });
});

// Health checks — used by the Docker Compose healthcheck directive
builder.Services.AddHealthChecks();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Rate Limiting for Login (in-process fallback, AspNetCoreRateLimit handles distributed below)
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

// ── AspNetCoreRateLimit — distributed rate limiting backed by Redis IDistributedCache ─────────
// Reads rules from appsettings.json [IpRateLimiting] section.
// Counters are stored in IDistributedCache (Redis in production, MemoryCache in dev).
builder.Services.AddMemoryCache(); // required by AspNetCoreRateLimit internals
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();
// ─────────────────────────────────────────────────────────────────────────────────────────────

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
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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
        ClockSkew = TimeSpan.FromMinutes(1),

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

builder.Services.Configure<S3Settings>(
    builder.Configuration.GetSection("AWS"));

// Food feature
builder.Services.AddScoped<IFoodShopRepository, FoodShopRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IFoodShopService, FoodShopService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<IImageService, ImageService>();

//testimonial
builder.Services.AddScoped<ITestimonialRepository, TestimonialRepository>();
builder.Services.AddScoped<ITestimonialService, TestimonialService>();


// Accommodation feature
builder.Services.AddScoped<IAccommodationRepository, AccommodationRepository>();
builder.Services.AddScoped<IAccommodationItemRepository, AccommodationItemRepository>();
builder.Services.AddScoped<IAccommodationService, AccommodationService>();
builder.Services.AddScoped<IAccommodationItemService, AccommodationItemService>();

//Transport 
builder.Services.AddScoped<ITukTukDriverRepository, TukTukDriverRepository>();
builder.Services.AddScoped<IBusRouteRepository, BusRouteRepository>();
builder.Services.AddScoped<ITrainRouteRepository, TrainRouteRepository>();
builder.Services.AddScoped<ITukTukDriverService, TukTukDriverService>();
builder.Services.AddScoped<IBusRouteService, BusRouteService>();
builder.Services.AddScoped<ITrainRouteService, TrainRouteService>();


// Gift feature
builder.Services.AddScoped<IGiftShopRepository, GiftShopRepository>();
builder.Services.AddScoped<IGiftShopService, GiftShopService>();

// Photography feature
builder.Services.AddScoped<IPhotographerRepository, PhotographerRepository>();
builder.Services.AddScoped<IPhotographerService, PhotographerService>();

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
builder.Services.Configure<ResendSettings>(builder.Configuration.GetSection("Resend"));
builder.Services.AddHttpClient<IEmailService, ResendEmailService>();
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

// Register the CacheService AFTER AddStackExchangeRedisCache / AddDistributedMemoryCache
// so IDistributedCache is already in the container.
builder.Services.AddSingleton<ICacheService, CacheService>();


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
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        
        // 1. MUST BE FIRST: Apply EF Core Migrations to create 'Users' and other tables
        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Database migration failed (possibly due to missing PostGIS extension locally). Continuing with fallback table creation...");
        }

        // 2. NOW raw SQL & seeding can safely run!
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""BusinessApplications"" (
                    ""Id"" text NOT NULL,
                    ""UserId"" text NOT NULL,
                    ""BusinessType"" text NOT NULL,
                    ""BusinessName"" text NOT NULL,
                    ""OwnerName"" text NOT NULL,
                    ""Phone"" text NOT NULL,
                    ""Address"" text NOT NULL,
                    ""Description"" text NOT NULL,
                    ""Status"" text NOT NULL DEFAULT 'Pending',
                    ""SubmittedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_BusinessApplications"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_BusinessApplications_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
                );
            ");

            try { dbContext.Database.ExecuteSqlRaw(@"ALTER TABLE ""BusinessApplications"" DROP COLUMN IF EXISTS ""RegistrationNumber"";"); } catch { }
            try { dbContext.Database.ExecuteSqlRaw(@"ALTER TABLE ""BusinessApplications"" DROP COLUMN IF EXISTS ""ApplicationDataJson"";"); } catch { }
            try { dbContext.Database.ExecuteSqlRaw(@"ALTER TABLE ""BusinessApplications"" ALTER COLUMN ""Id"" TYPE text USING ""Id""::text;"); } catch { }
            try { dbContext.Database.ExecuteSqlRaw(@"ALTER TABLE ""FoodShops"" ADD COLUMN IF NOT EXISTS ""OwnerId"" text;"); } catch { }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "BusinessApplications table initialization non-fatal warning");
        }

        // Ensure GiftShop tables exist in case EF Migrations History is out of sync
        dbContext.Database.ExecuteSqlRaw(@"
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

            CREATE TABLE IF NOT EXISTS ""Deals"" (
                ""Id"" text NOT NULL,
                ""ShopName"" character varying(100) NOT NULL,
                ""ShopType"" character varying(50) NOT NULL,
                ""Title"" character varying(150) NOT NULL,
                ""Description"" character varying(1000) NOT NULL,
                ""BadgeText"" character varying(50) NOT NULL,
                ""BadgeColor"" character varying(20) NOT NULL,
                ""ImageUrl"" character varying(500),
                ""ValidFrom"" timestamp with time zone,
                ""ValidTo"" timestamp with time zone,
                ""SubmittedByUserId"" text NOT NULL,
                ""ApprovalStatus"" character varying(50) NOT NULL DEFAULT 'Pending',
                ""RejectionReason"" character varying(500),
                ""CreatedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_Deals"" PRIMARY KEY (""Id""),
                CONSTRAINT ""FK_Deals_Users_SubmittedByUserId"" FOREIGN KEY (""SubmittedByUserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ""IX_Deals_SubmittedByUserId"" ON ""Deals"" (""SubmittedByUserId"");
        ");

        dbContext.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""Photographers"" (
                ""Id"" uuid NOT NULL,
                ""Name"" character varying(150) NOT NULL,
                ""Bio"" character varying(500),
                ""BaseRatePerHour"" numeric(18,2) NOT NULL,
                ""LocationName"" character varying(150) NOT NULL,
                ""Phone"" character varying(20) NOT NULL,
                ""Email"" character varying(150),
                ""ImageUrl"" character varying(500),
                ""IsActive"" boolean NOT NULL DEFAULT TRUE,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NOT NULL,
                ""OwnerId"" text,
                CONSTRAINT ""PK_Photographers"" PRIMARY KEY (""Id"")
            );

            CREATE TABLE IF NOT EXISTS ""PhotographyPackages"" (
                ""Id"" uuid NOT NULL,
                ""PhotographerId"" uuid NOT NULL,
                ""Name"" character varying(150) NOT NULL,
                ""Price"" numeric(18,2) NOT NULL,
                ""Description"" character varying(300),
                ""IsActive"" boolean NOT NULL DEFAULT TRUE,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_PhotographyPackages"" PRIMARY KEY (""Id""),
                CONSTRAINT ""FK_PhotographyPackages_Photographers_PhotographerId"" FOREIGN KEY (""PhotographerId"") REFERENCES ""Photographers"" (""Id"") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ""IX_PhotographyPackages_PhotographerId"" ON ""PhotographyPackages"" (""PhotographerId"");
        ");

        // Your seeding logic follows here...
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

// Map OpenAPI document and Scalar API Reference UI globally (enabled in production)
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("NearU API Documentation")
           .WithTheme(ScalarTheme.Purple)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// HTTPS is handled at the Nginx reverse proxy level (nginx.conf HTTP→HTTPS redirect).
// Do NOT call UseHttpsRedirection() here — the backend only receives HTTP from the
// Nginx upstream and redirecting would cause redirect loops in production.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Trust X-Forwarded-For and X-Forwarded-Proto from Nginx reverse proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Enable CORS BEFORE routing and rate limiting so all responses (including errors & rejections) include CORS headers
app.UseCors("AllowFrontend");

app.UseRouting();

// Distributed IP rate limiting (AspNetCoreRateLimit — Redis-backed in production)
app.UseIpRateLimiting();
app.UseRateLimiter();
app.UseAuthentication();
// Token blacklist check — runs after UseAuthentication so ClaimsPrincipal is populated.
// Rejects requests whose JWT jti is in the Redis blacklist (e.g. after logout).
app.UseMiddleware<TokenBlacklistMiddleware>();
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
