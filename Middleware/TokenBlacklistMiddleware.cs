using System.IdentityModel.Tokens.Jwt;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Services;

namespace NearU_Backend_Revised.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware that checks authenticated requests against the Redis JWT blacklist.
    /// Must be registered AFTER UseAuthentication() so ClaimsPrincipal is populated.
    /// Returns 401 with a standard ApiResponse body if the token's jti is on the blacklist (e.g. after logout).
    /// </summary>
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenBlacklistMiddleware> _logger;

        public TokenBlacklistMiddleware(RequestDelegate next, ILogger<TokenBlacklistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
        {
            // Only check authenticated requests that carry a Bearer token
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrWhiteSpace(jti))
                {
                    bool isBlacklisted;
                    try
                    {
                        isBlacklisted = await tokenService.IsTokenBlacklistedAsync(jti);
                    }
                    catch (Exception ex)
                    {
                        // Redis is unavailable — fail-open (log and continue) so a cache outage
                        // does not lock every authenticated user out of the API.
                        _logger.LogError(ex, "Redis unavailable during blacklist check for jti={Jti}. Failing open.", jti);
                        await _next(context);
                        return;
                    }

                    if (isBlacklisted)
                    {
                        _logger.LogWarning("Rejected blacklisted token jti={Jti} for path={Path}.", jti, context.Request.Path);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(
                            ApiResponse<object>.FailResponse("Token has been revoked. Please log in again."));
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
