using System.Runtime.CompilerServices;
using Cachula.Interfaces;
using Microsoft.Extensions.Caching.Memory;

[assembly: InternalsVisibleTo("Cachula.Tests")]

namespace Cachula.Layers;

/// <summary>
/// CachulaMemoryCache is a memory cache layer that uses IMemoryCache.
/// </summary>
internal class CachulaMemoryCache : ICacheLayer
{
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachulaMemoryCache"/> class.
    /// </summary>
    /// <param name="cache">The IMemoryCache instance to use.</param>
    /// <remarks>
    /// This constructor requires an instance of <see cref="IMemoryCache"/> from Microsoft.Extensions.Caching.Memory.
    /// It is typically registered in the DI container.
    /// </remarks>
    public CachulaMemoryCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Gets a cached entry by key.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>
    /// A <see cref="CachulaCacheEntry{T}"/> containing the cached value or a miss indication.
    /// </returns>
    public CachulaCacheEntry<T> Get<T>(string key)
    {
        if (_cache.TryGetValue<T>(key, out var value))
        {
            // TTL is not used for now in MemoryCache, so we set it to zero.
            return value == null ? CachulaCacheEntry<T>.Null : new CachulaCacheEntry<T>(value, TimeSpan.Zero);
        }

        return CachulaCacheEntry<T>.Missed;
    }

    /// <inheritdoc />
    public Task<CachulaCacheEntry<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Get<T>(key));
    }

    /// <summary>
    /// Gets multiple cached entries by keys.
    /// </summary>
    /// <typeparam name="T">The type of the cached values.</typeparam>
    /// <param name="keys">The cache keys.</param>
    /// <returns>
    /// A dictionary mapping keys to <see cref="CachulaCacheEntry{T}"/> instances.
    /// </returns>
    public IDictionary<string, CachulaCacheEntry<T>> GetMany<T>(IEnumerable<string> keys)
    {
        var dict = new Dictionary<string, CachulaCacheEntry<T>>();
        foreach (var key in keys)
        {
            dict[key] = Get<T>(key);
        }

        return dict;
    }

    /// <inheritdoc />
    public Task<IDictionary<string, CachulaCacheEntry<T>>> GetManyAsync<T>(
        IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetMany<T>(keys));
    }

    /// <summary>
    /// Sets a cached entry with a specified expiration time.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">The expiration time for the cached entry.</param>
    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        _cache.Set(key, value!, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        Set(key, value, expiration);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetManyAsync<T>(IDictionary<string, T> values, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        foreach (var kvp in values)
        {
            Set(kvp.Key, kvp.Value, ttl);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
        {
            _cache.Remove(key);
        }

        return Task.CompletedTask;
    }
}
