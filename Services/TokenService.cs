using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NearU_Backend_Revised.Configuration;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NearU_Backend_Revised.Services
{
    /// <summary>
    /// Service for generating and validating JWT access tokens and refresh tokens
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly Interfaces.ICacheService _cache;

        private const string BlacklistKeyPrefix  = "nearu:jti:blacklist:";
        // Key pattern for the per-user active-JTI Set: nearu:user:jtis:{userId}:set
        private const string UserJtiSetKeyPrefix = "nearu:user:jtis:";

        public TokenService(
            IOptions<JwtSettings> jwtSettings,
            IRefreshTokenRepository refreshTokenRepository,
            Interfaces.ICacheService cache)
        {
            _jwtSettings = jwtSettings.Value;
            _refreshTokenRepository = refreshTokenRepository;
            _cache = cache;
        }

        /// <summary>
        /// Generate a JWT access token for a user
        /// Includes claims: userId, email, username, role
        /// </summary>
        public string GenerateAccessToken(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Create claims for the JWT token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("userId", user.Id), // Custom claim for easier access
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique token ID
            };

            // Create signing credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryInMinutes),
                SigningCredentials = credentials,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience
            };

            // Generate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Track this jti in the user's active-JTI set (best-effort, fire-and-forget)
            var jti = token.Id; // same Guid added to claims above
            var tokenLifetime = TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpiryInMinutes);
            // Do NOT await — GenerateAccessToken is synchronous; tracking failure must not block the caller.
            _ = _cache.SetAddAsync(UserJtiSetKeyPrefix + user.Id, jti, tokenLifetime);

            return tokenString;
        }

        /// <summary>
        /// Generate a secure refresh token
        /// Returns a RefreshToken entity with all necessary properties
        /// </summary>
        public RefreshToken GenerateRefreshToken(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            // Generate a cryptographically secure random token
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var token = Convert.ToBase64String(randomBytes);

            // Create refresh token entity
            var refreshToken = new RefreshToken
            {
                Token = token,
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryInDays)
            };

            return refreshToken;
        }

        /// <summary>
        /// Validate a JWT access token and extract the user ID
        /// </summary>
        public string? ValidateAccessToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // No tolerance for expiration
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Extract user ID from claims
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim?.Value;
            }
            catch (SecurityTokenException)
            {
                // Token validation failed
                return null;
            }
            catch (Exception)
            {
                // Other errors
                return null;
            }
        }

        /// <summary>
        /// Extract claims from a JWT token without validation (for debugging)
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var claims = jwtToken.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
                var identity = new ClaimsIdentity(claims);
                var principal = new ClaimsPrincipal(identity);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validate a refresh token
        /// Checks if token exists, is not expired, and is not revoked
        /// </summary>
        public async Task<RefreshToken?> ValidateRefreshToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            // Get token from database
            var refreshToken = await _refreshTokenRepository.GetRefreshTokenByTokenStringAsync(token);

            if (refreshToken == null)
                return null;

            // Check if token is expired
            if (refreshToken.ExpiryDate < DateTime.UtcNow)
                return null;

            // Check if token is revoked
            if (refreshToken.IsRevoked)
                return null;

            // Token is valid
            return refreshToken;
        }

        /// <summary>
        /// Rotate a refresh token (revoke old token and generate new one)
        /// </summary>
        public async Task<RefreshToken?> RotateRefreshToken(string oldToken)
        {
            if (string.IsNullOrEmpty(oldToken))
                return null;

            // Validate the old token first
            var validToken = await ValidateRefreshToken(oldToken);
            if (validToken == null)
                return null;

            // Generate new refresh token
            var newRefreshToken = GenerateRefreshToken(validToken.UserId);

            // Replace old token with new one in database
            var replacedToken = await _refreshTokenRepository.ReplaceRefreshTokenAsync(oldToken, newRefreshToken);

            return replacedToken;
        }
        /// <summary>Blacklists the given jti in Redis so the token cannot be reused.</summary>
        public async Task BlacklistTokenAsync(string jti, TimeSpan remaining)
        {
            if (string.IsNullOrWhiteSpace(jti)) return;
            // Store a sentinel value — the key existence is the only thing checked.
            await _cache.SetAsync($"{BlacklistKeyPrefix}{jti}", "revoked", remaining);
            // Also remove from the user's active-JTI set. We don't know the userId here
            // so the set clean-up happens on the caller side (AuthController).
        }

        /// <summary>Returns true if the jti is on the Redis blacklist.</summary>
        public async Task<bool> IsTokenBlacklistedAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti)) return false;
            return await _cache.ExistsAsync($"{BlacklistKeyPrefix}{jti}");
        }

        // ── Sign-Out-All-Devices ────────────────────────────────────────────────────────

        /// <summary>
        /// Records the newly issued jti in the user's active-JTI Set.
        /// Called automatically from <see cref="GenerateAccessToken"/> (fire-and-forget).
        /// </summary>
        public async Task TrackActiveTokenAsync(string userId, string jti, TimeSpan tokenLifetime)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(jti)) return;
            // TTL of the set = token lifetime so it self-expires when no tokens are active.
            await _cache.SetAddAsync(UserJtiSetKeyPrefix + userId, jti, tokenLifetime);
        }

        /// <summary>
        /// Sign-Out-All-Devices: reads every tracked jti for <paramref name="userId"/>,
        /// blacklists each one, then removes the Set.
        /// </summary>
        public async Task BlacklistAllUserTokensAsync(string userId, TimeSpan tokenLifetime)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var setKey = UserJtiSetKeyPrefix + userId;
            var jtis = await _cache.SetMembersAsync(setKey);

            var tasks = jtis.Select(jti =>
                _cache.SetAsync($"{BlacklistKeyPrefix}{jti}", "revoked", tokenLifetime));

            await Task.WhenAll(tasks);

            // Wipe the entire Set so it cannot be iterated again
            await _cache.RemoveAsync($"{setKey}:set");
        }
    }
}
