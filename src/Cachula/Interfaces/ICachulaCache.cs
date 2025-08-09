namespace Cachula.Interfaces;

/// <summary>
/// Interface for Cachula cache operations.
/// </summary>
public interface ICachulaCache
{
    /// <summary>
    /// Gets or sets a value in the cache with a specified expiration time.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">A function that generates the value to cache.</param>
    /// <param name="expiration">The expiration time for the cache entry.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the cached value.</returns>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets multiple values in the cache with a specified expiration time.
    /// </summary>
    /// <param name="keys">The cache keys.</param>
    /// <param name="factory">A function that generates the values to cache.</param>
    /// <param name="expiration">The expiration time for the cache entries.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only collection of cached values.
    /// </returns>
    /// <typeparam name="T">The type of the cached values.</typeparam>
    Task<IReadOnlyCollection<T>> GetOrSetManyAsync<T>(
        IEnumerable<string> keys,
        Func<IEnumerable<string>, CancellationToken, Task<IDictionary<string, T>>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets multiple values in the cache and returns them as a dictionary.
    /// </summary>
    /// <param name="keys">The cache keys.</param>
    /// <param name="factory">A function that generates the values to cache.</param>
    /// <param name="expiration">The expiration time for the cache entries.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a dictionary of cached values.
    /// </returns>
    /// <typeparam name="T">The type of the cached values.</typeparam>
    Task<Dictionary<string, T>> GetOrSetManyAsyncAsDict<T>(
        IEnumerable<string> keys,
        Func<IEnumerable<string>, CancellationToken, Task<IDictionary<string, T>>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}
