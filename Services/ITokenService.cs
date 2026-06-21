using NearU_Backend_Revised.Models;

namespace NearU_Backend_Revised.Services
{
    /// <summary>Interface for JWT generation, validation, and lifecycle management.</summary>
    public interface ITokenService
    {
        // ── Generation ──────────────────────────────────────────────────────────────────

        /// <summary>Generates a signed JWT access token for the given user.</summary>
        string GenerateAccessToken(User user);

        /// <summary>Generates a cryptographically-random, opaque refresh token.</summary>
        RefreshToken GenerateRefreshToken(string userId);

        // ── Validation ──────────────────────────────────────────────────────────────────

        /// <summary>Validates a JWT and returns the userId claim, or null if invalid.</summary>
        string? ValidateAccessToken(string token);

        /// <summary>Validates an opaque refresh token (DB lookup — not expired, not revoked).</summary>
        Task<RefreshToken?> ValidateRefreshToken(string token);

        // ── Rotation ────────────────────────────────────────────────────────────────────

        /// <summary>Revokes the old refresh token and issues a new one.</summary>
        Task<RefreshToken?> RotateRefreshToken(string oldToken);

        // ── Per-device blacklist ─────────────────────────────────────────────────────────

        /// <summary>
        /// Adds the given JWT token ID (jti claim) to the Redis blacklist.
        /// The entry is automatically expired after <paramref name="remaining"/> elapses.
        /// Called on standard single-device logout.
        /// </summary>
        Task BlacklistTokenAsync(string jti, TimeSpan remaining);

        /// <summary>Returns true when the given jti is on the Redis blacklist.</summary>
        Task<bool> IsTokenBlacklistedAsync(string jti);

        // ── Sign-Out-All-Devices ─────────────────────────────────────────────────────────

        /// <summary>
        /// Tracks the newly issued jti in the user's active-JTI Redis Set.
        /// Called by <see cref="GenerateAccessToken"/> so that Sign-Out-All-Devices can
        /// enumerate and revoke every outstanding token for a user.
        /// </summary>
        Task TrackActiveTokenAsync(string userId, string jti, TimeSpan tokenLifetime);

        /// <summary>
        /// Revokes every active access token for <paramref name="userId"/> by:
        ///   1. Reading the user's JTI Set from Redis.
        ///   2. Adding each jti to the blacklist.
        ///   3. Deleting the Set.
        /// </summary>
        Task BlacklistAllUserTokensAsync(string userId, TimeSpan tokenLifetime);
    }
}
