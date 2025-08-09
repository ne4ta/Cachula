using Cachula.Layers;

namespace Cachula.Interfaces;

/// <summary>
/// Represents a cache layer interface for managing cached values.
/// </summary>
public interface ICacheLayer
{
    /// <summary>
    /// Gets a value from the cache using a single key asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="key">The key to use for retrieving the cached value.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the cached value, or default if not found.</returns>
    Task<CachulaCacheEntry<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from the cache using multiple keys asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="keys">The keys to use for retrieving the cached value.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the cached value, or default if not found.</returns>
    Task<IDictionary<string, CachulaCacheEntry<T>>> GetManyAsync<T>(
        IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache using a single key asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The key to use for caching.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">The expiration time for the cached value.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets multiple values in the cache using a dictionary of keys and values asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the values to cache.</typeparam>
    /// <param name="values">A dictionary containing keys and their corresponding values to cache.</param>
    /// <param name="ttl">The time-to-live for the cached values.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetManyAsync<T>(IDictionary<string, T> values, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple values from the cache using a collection of keys asynchronously.
    /// </summary>
    /// <param name="keys">The keys of the values to remove from the cache.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}
