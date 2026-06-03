using System.Text.Json.Serialization.Metadata;

namespace NearU_Backend_Revised.Services.Interfaces
{
    /// <summary>
    /// Generic distributed cache abstraction over IDistributedCache.
    ///
    /// Two serialisation paths:
    ///   • Generic <c>GetAsync&lt;T&gt; / SetAsync&lt;T&gt;</c> — reflection-based, fine for non-AOT.
    ///   • Typed <c>GetAsync&lt;T&gt;(key, JsonTypeInfo&lt;T&gt;) / SetAsync&lt;T&gt;(key, value, JsonTypeInfo&lt;T&gt;)</c>
    ///     — source-generated, safe under Native AOT and IL trimming.
    ///
    /// Redis Set operations support the Sign-Out-All-Devices mechanism.
    /// </summary>
    public interface ICacheService
    {
        // ── Standard (reflection-safe) ──────────────────────────────────────────────────

        /// <summary>Returns the cached value for <paramref name="key"/>, or null if missing/expired.</summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>Stores <paramref name="value"/> under <paramref name="key"/> with an optional TTL.</summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

        // ── AOT-safe typed overloads ────────────────────────────────────────────────────

        /// <summary>
        /// AOT-safe read. Pass a <see cref="JsonTypeInfo{T}"/> from the source-generated
        /// <c>NearUJsonContext</c> (e.g. <c>NearUJsonContext.Default.FoodShopResponse</c>).
        /// </summary>
        Task<T?> GetAsync<T>(string key, JsonTypeInfo<T> typeInfo);

        /// <summary>
        /// AOT-safe write. Pass a <see cref="JsonTypeInfo{T}"/> from the source-generated
        /// <c>NearUJsonContext</c>.
        /// </summary>
        Task SetAsync<T>(string key, T value, JsonTypeInfo<T> typeInfo, TimeSpan? expiry = null);

        // ── Shared ──────────────────────────────────────────────────────────────────────

        /// <summary>Removes the entry with the given <paramref name="key"/>.</summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Checks whether a key exists in the cache.
        /// Used for lightweight existence checks such as the JWT blacklist.
        /// </summary>
        Task<bool> ExistsAsync(string key);

        // ── Redis Set operations (Sign-Out-All-Devices) ─────────────────────────────────

        /// <summary>
        /// Adds <paramref name="member"/> to the logical Redis Set stored under <paramref name="key"/>.
        /// Used to track all active JTIs for a user so that Sign-Out-All-Devices can revoke them.
        /// </summary>
        Task SetAddAsync(string key, string member, TimeSpan? expiry = null);

        /// <summary>Returns all members of the logical Redis Set at <paramref name="key"/>.</summary>
        Task<IEnumerable<string>> SetMembersAsync(string key);

        /// <summary>Removes a single <paramref name="member"/> from the Redis Set (called on standard logout).</summary>
        Task SetRemoveAsync(string key, string member);
    }
}
