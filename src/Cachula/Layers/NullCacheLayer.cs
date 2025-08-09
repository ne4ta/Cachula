using Cachula.Interfaces;

namespace Cachula.Layers;

/// <summary>
/// NullCacheLayer is an implementation of ICacheLayer that does not store or return any data.
/// Used as a "stub" to disable a cache layer without changing CachulaEngine logic.
/// All read methods always return empty/null values, and write/remove methods do nothing.
/// </summary>
internal class NullCacheLayer : ICacheLayer
{
    /// <summary>
    /// Always returns a null cache entry.
    /// </summary>
    /// <inheritdoc />
    public Task<CachulaCacheEntry<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(CachulaCacheEntry<T>.Missed);

    /// <summary>
    /// Always returns a dictionary where each key maps to a null cache entry.
    /// </summary>
    /// <inheritdoc />
    public Task<IDictionary<string, CachulaCacheEntry<T>>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        => Task.FromResult<IDictionary<string, CachulaCacheEntry<T>>>(keys.ToDictionary(k => k, _ => CachulaCacheEntry<T>.Missed));

    /// <summary>
    /// Does nothing when trying to set a value by key.
    /// </summary>
    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Does nothing when trying to set multiple values.
    /// </summary>
    /// <inheritdoc />
    public Task SetManyAsync<T>(IDictionary<string, T> values, TimeSpan ttl, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Does nothing when trying to remove keys.
    /// </summary>
    /// <inheritdoc />
    public Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
