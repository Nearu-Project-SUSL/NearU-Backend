using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NearU_Backend_Revised.Serialization;

namespace NearU_Backend_Revised.Services
{
    /// <summary>
    /// Redis-backed <see cref="Interfaces.ICacheService"/> using the source-generated
    /// <see cref="NearUJsonContext"/> for all serialisation — AOT / IL-trim safe.
    ///
    /// Design goals:
    ///   • Never throw — cache failures fall through to the source (DB).
    ///   • Singleton lifetime — mirrors <see cref="IDistributedCache"/>.
    ///   • JsonTypeInfo overloads prevent reflection-based serialisation under Native AOT.
    /// </summary>
    public class CacheService : Interfaces.ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        // ─── Generic helpers (reflection-based — safe for non-AOT deployments) ──────────

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var bytes = await _cache.GetAsync(key);
                if (bytes is null || bytes.Length == 0) return default;
                return JsonSerializer.Deserialize<T>(bytes, NearUJsonContext.Default.Options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache GET failed for key '{Key}'. Falling through to source.", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, NearUJsonContext.Default.Options);
                await _cache.SetAsync(key, bytes, BuildOptions(expiry));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache SET failed for key '{Key}'. Continuing without caching.", key);
            }
        }

        // ─── AOT-safe typed overloads (preferred in hot paths) ───────────────────────────

        /// <summary>AOT-safe read using a pre-compiled <see cref="JsonTypeInfo{T}"/>.</summary>
        public async Task<T?> GetAsync<T>(string key, JsonTypeInfo<T> typeInfo)
        {
            try
            {
                var bytes = await _cache.GetAsync(key);
                if (bytes is null || bytes.Length == 0) return default;
                return JsonSerializer.Deserialize(bytes, typeInfo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache GET (typed) failed for key '{Key}'.", key);
                return default;
            }
        }

        /// <summary>AOT-safe write using a pre-compiled <see cref="JsonTypeInfo{T}"/>.</summary>
        public async Task SetAsync<T>(string key, T value, JsonTypeInfo<T> typeInfo, TimeSpan? expiry = null)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeInfo);
                await _cache.SetAsync(key, bytes, BuildOptions(expiry));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache SET (typed) failed for key '{Key}'.", key);
            }
        }

        // ─── Shared operations ────────────────────────────────────────────────────────────

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache REMOVE failed for key '{Key}'.", key);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var bytes = await _cache.GetAsync(key);
                return bytes is not null && bytes.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache EXISTS check failed for key '{Key}'.", key);
                return false;
            }
        }

        // ─── Redis Set operations (used for Sign-Out-All-Devices) ─────────────────────────

        /// <summary>
        /// Adds <paramref name="member"/> to the Redis Set at <paramref name="key"/>.
        /// Falls back silently on error (token tracking is best-effort).
        /// </summary>
        public async Task SetAddAsync(string key, string member, TimeSpan? expiry = null)
        {
            // IDistributedCache has no native SADD — we simulate a Set via a JSON-serialised HashSet.
            // For a true Redis SADD you would inject IConnectionMultiplexer; this implementation
            // is compatible with the existing IDistributedCache abstraction.
            try
            {
                var setKey = $"{key}:set";
                var existing = await GetAsync<HashSet<string>>(setKey) ?? new HashSet<string>();
                existing.Add(member);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(existing, NearUJsonContext.Default.Options);
                await _cache.SetAsync(setKey, bytes, BuildOptions(expiry));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache SET_ADD failed for key '{Key}', member '{Member}'.", key, member);
            }
        }

        /// <summary>Returns all members of the Redis Set stored at <paramref name="key"/>.</summary>
        public async Task<IEnumerable<string>> SetMembersAsync(string key)
        {
            try
            {
                var setKey = $"{key}:set";
                return await GetAsync<HashSet<string>>(setKey) ?? Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache SET_MEMBERS failed for key '{Key}'.", key);
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>Removes <paramref name="member"/> from the Redis Set.</summary>
        public async Task SetRemoveAsync(string key, string member)
        {
            try
            {
                var setKey = $"{key}:set";
                var existing = await GetAsync<HashSet<string>>(setKey);
                if (existing is null || !existing.Contains(member)) return;
                existing.Remove(member);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(existing, NearUJsonContext.Default.Options);
                await _cache.SetAsync(setKey, bytes, BuildOptions(null));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache SET_REMOVE failed for key '{Key}', member '{Member}'.", key, member);
            }
        }

        // ─── Private helpers ──────────────────────────────────────────────────────────────

        private static DistributedCacheEntryOptions BuildOptions(TimeSpan? expiry)
        {
            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
                options.AbsoluteExpirationRelativeToNow = expiry;
            return options;
        }
    }
}
