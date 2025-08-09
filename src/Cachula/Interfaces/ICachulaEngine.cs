using Cachula.Layers;

namespace Cachula.Interfaces;

/// <summary>
/// Interface for the Cachula caching engine, providing methods to interact with a multi-layered cache system.
/// It supports retrieving, setting, and removing cached values across multiple layers with stampede protection.
/// </summary>
public interface ICachulaEngine
{
    /// <summary>
    /// Gets a value by key from the cache hierarchy. First checks the fastest (first) cache layer. If not found,
    /// uses stampede protection to ensure only one concurrent loader per key. Within the protected section, checks
    /// all lower (slower) cache layers in order. If found, warms all higher (faster) layers above the found one.
    /// If not found in any cache, loads the value via the provided factory and writes it to all cache layers.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">The key to retrieve the value for.</param>
    /// <param name="valueFactory">A factory function to load the value if not found in any cache layer.</param>
    /// <param name="ttl">Time-to-live for the cached value.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>The cached value or null if not found.</returns>
    Task<CachulaCacheEntry<T>> GetOrSetAsync<T>(
        string key, Func<Task<T?>> valueFactory, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value by key in all configured layers.
    /// </summary>
    /// <typeparam name="T">Type of the value to set.</typeparam>
    /// <param name="key">The key to set the value for.</param>
    /// <param name="value">The value to set in the cache.</param>
    /// <param name="ttl">Time-to-live for the cached value.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple values by keys from the cache hierarchy. First checks the fastest (first) cache layer. If not found,
    /// uses stampede protection to ensure only one concurrent loader per key. Within the protected section, checks
    /// all lower (slower) cache layers in order for each missing key. If found, warms all higher (faster) layers above the found one.
    /// If not found in any cache, loads the value via the provided factory and writes it to all cache layers.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="keys">The keys to retrieve the values for.</param>
    /// <param name="valueFactory">A factory function to load the values if not found in any cache layer.</param>
    /// <param name="ttl">Time-to-live for the cached values.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>A dictionary containing the keys and their corresponding cached values, or null if not found.</returns>
    Task<IDictionary<string, CachulaCacheEntry<T>>> GetOrSetManyAsync<T>(
        IEnumerable<string> keys,
        Func<IEnumerable<string>, Task<IDictionary<string, T>>> valueFactory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets multiple key-value pairs in all configured layers in parallel.
    /// </summary>
    /// <typeparam name="T">Type of the values to set.</typeparam>
    /// <param name="values">A dictionary containing keys and their corresponding values to set in the cache.</param>
    /// <param name="ttl">Time-to-live for the cached values.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetManyAsync<T>(IDictionary<string, T> values, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple keys from all configured layers in parallel.
    /// </summary>
    /// <param name="keys">Keys to remove.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}
