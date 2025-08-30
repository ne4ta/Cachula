using Cachula.Interfaces;

namespace Cachula;

/// <summary>
/// Implementation of the ICachulaCache interface that provides caching functionality.
/// </summary>
public class CachulaCache(ICachulaEngine engine) : ICachulaCache
{
    /// <inheritdoc />
    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var entry = await engine.GetOrSetAsync(
            key,
            async () => await factory(cancellationToken),
            expiration,
            cancellationToken);

        if (!entry.IsNull)
        {
            return entry.Value;
        }

        return default;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<T>> GetOrSetManyAsync<T>(
        IEnumerable<string> keys,
        Func<IEnumerable<string>, CancellationToken, Task<IDictionary<string, T>>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var dict = await GetOrSetManyAsyncAsDict(keys, factory, expiration, cancellationToken);
        return dict.Values;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, T>> GetOrSetManyAsyncAsDict<T>(
        IEnumerable<string> keys,
        Func<IEnumerable<string>, CancellationToken, Task<IDictionary<string, T>>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var entries = await engine.GetOrSetManyAsync(
            keys,
            async missingKeys => await factory(missingKeys, cancellationToken),
            expiration,
            cancellationToken);

        var result = new Dictionary<string, T>(entries.Count, StringComparer.Ordinal);
        foreach (var (key, entry) in entries)
        {
            if (!entry.IsNull)
            {
                result[key] = entry.Value!;
            }
        }

        return result;
    }
}
