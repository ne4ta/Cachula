using System.Runtime.CompilerServices;
using Cachula.Interfaces;
using Cachula.Layers;

[assembly: InternalsVisibleTo("Cachula.Tests")]

namespace Cachula.Core;

/// <summary>
/// CachulaEngine is the main engine for managing multiple cache layers.
/// It provides methods to get, set, and remove cached values across all configured layers.
/// It also includes stampede protection to prevent multiple concurrent loads for the same key.
/// </summary>
internal class CachulaEngine : ICachulaEngine
{
    private readonly IReadOnlyList<ICacheLayer> _layers;
    private readonly IStampedeProtector _protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachulaEngine"/> class.
    /// </summary>
    /// <param name="layers">An ordered list of cache layers (e.g., Memory -> Redis).</param>
    /// <param name="protector">Protector to avoid cache stampede per key.</param>
    public CachulaEngine(IEnumerable<ICacheLayer> layers, IStampedeProtector protector)
    {
        _layers = layers?.ToList() ?? throw new ArgumentNullException(nameof(layers));
        if (!_layers.Any())
        {
            throw new ArgumentException("At least one cache layer is required.", nameof(layers));
        }

        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
    }

    /// <inheritdoc />
    public async Task<CachulaCacheEntry<T>> GetOrSetAsync<T>(
        string key, Func<Task<T?>> valueFactory, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (valueFactory == null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        var firstLayer = _layers[0];
        var entry = await firstLayer.GetAsync<T>(key, cancellationToken);
        if (!entry.IsMissed)
        {
            return entry;
        }

        return await _protector.RunAsync(key, async () =>
        {
            for (var i = 1; i < _layers.Count; i++)
            {
                var ent = await _layers[i].GetAsync<T>(key, cancellationToken);
                if (!ent.IsMissed)
                {
                    for (var j = 0; j < i; j++)
                    {
                        await _layers[j].SetAsync(key, ent, ttl, cancellationToken);
                    }

                    return ent;
                }
            }

            var loaded = await valueFactory();

            await WarmLayersAsync(key, loaded, ttl, cancellationToken);

            return new CachulaCacheEntry<T>(loaded, ttl);
        });
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var tasks = _layers.Select(layer => layer.SetAsync(key, value, ttl, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, CachulaCacheEntry<T>>> GetOrSetManyAsync<T>(
        IEnumerable<string> keys,
        Func<IEnumerable<string>, Task<IDictionary<string, T>>> valueFactory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }
        
        if (valueFactory == null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        var keyArray = keys.Distinct().ToArray();

        var (found, missing) = await FindInMemoryAsync<T>(_layers[0], keyArray, cancellationToken);
        if (missing.Count == 0)
        {
            return found;
        }

        var tasks = _protector.RunManyAsync<CachulaCacheEntry<T>>(missing, async toLoad =>
        {
            var (loaded, foundAtLayer, toFind) = await FindInLowerLayersAsync<T>(toLoad, cancellationToken);
            await WarmLayersAboveFoundLevelAsync(loaded, foundAtLayer, ttl, cancellationToken);

            var stillMissing = toFind.ToList();
            if (stillMissing.Count > 0)
            {
                var toCache = await LoadFromFactory(stillMissing, valueFactory, loaded, ttl);
                if (toCache.Count > 0)
                {
                    await WarmLayersAsync(toCache, ttl, cancellationToken);
                }
            }

            return loaded;
        });

        foreach (var key in missing)
        {
            found[key] = await tasks[key];
        }

        return found;
    }

    /// <inheritdoc />
    public async Task SetManyAsync<T>(
        IDictionary<string, T> values, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }
        
        if (!values.Any())
        {
            return;
        }

        var tasks = _layers.Select(layer => layer.SetManyAsync(values, ttl, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        var distinctKeys = keys.Distinct().ToList();
        if (distinctKeys.Count == 0)
        {
            return;
        }

        var tasks = _layers.Select(layer => layer.RemoveManyAsync(distinctKeys, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private static async Task<(Dictionary<string, CachulaCacheEntry<T>> Found, List<string> Missing)> FindInMemoryAsync<T>(
        ICacheLayer cacheLayer,
        string[] keys,
        CancellationToken cancellationToken)
    {
        var cacheEntries = await cacheLayer.GetManyAsync<T>(keys, cancellationToken);
        var missing = new List<string>();
        var found = new Dictionary<string, CachulaCacheEntry<T>>();

        foreach (var key in keys)
        {
            if (cacheEntries.TryGetValue(key, out var v) && !v.IsMissed)
            {
                found[key] = v;
            }
            else
            {
                missing.Add(key);
            }
        }

        return (found, missing);
    }

    private static async Task<Dictionary<string, T>> LoadFromFactory<T>(
        List<string> stillMissing,
        Func<IEnumerable<string>, Task<IDictionary<string, T>>> valueFactory,
        Dictionary<string, CachulaCacheEntry<T>> loaded,
        TimeSpan ttl)
    {
        var factoryResult = await valueFactory(stillMissing);
        var toCache = new Dictionary<string, T>();
        foreach (var key in stillMissing)
        {
            if (factoryResult.TryGetValue(key, out var v) && v != null)
            {
                loaded[key] = new CachulaCacheEntry<T>(v, ttl);
                toCache[key] = v;
            }
            else
            {
                loaded[key] = CachulaCacheEntry<T>.Null;
            }
        }

        return toCache;
    }

    private async Task<(Dictionary<string, CachulaCacheEntry<T>> Loaded, Dictionary<string, int> FoundAtLayer, List<string> ToFind)> FindInLowerLayersAsync<T>(
        IEnumerable<string> toLoad, CancellationToken cancellationToken)
    {
        var toFind = toLoad.ToList();
        var loaded = new Dictionary<string, CachulaCacheEntry<T>>();
        var foundAtLayer = new Dictionary<string, int>();
        for (var i = 1; i < _layers.Count && toFind.Count > 0; i++)
        {
            var cacheEntries = await _layers[i].GetManyAsync<T>(toFind, cancellationToken);
            var found = new List<string>();
            foreach (var key in toFind)
            {
                if (cacheEntries.TryGetValue(key, out var v) && !v.IsMissed)
                {
                    loaded[key] = v;
                    foundAtLayer[key] = i;
                    found.Add(key);
                }
            }

            toFind = toFind.Except(found).ToList();
        }

        return (loaded, foundAtLayer, toFind);
    }

    private async Task WarmLayersAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
    {
        foreach (var layer in _layers)
        {
            await layer.SetAsync(key, value, ttl, cancellationToken);
        }
    }

    private async Task WarmLayersAsync<T>(IDictionary<string, T> values, TimeSpan ttl, CancellationToken cancellationToken)
    {
        foreach (var layer in _layers)
        {
            await layer.SetManyAsync(values, ttl, cancellationToken);
        }
    }

    private async Task WarmLayersAboveFoundLevelAsync<T>(
        Dictionary<string, CachulaCacheEntry<T>> loaded,
        Dictionary<string, int> foundAtLayer,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        foreach (var group in foundAtLayer.GroupBy(kvp => kvp.Value))
        {
            var foundKeys = group.Select(kvp => kvp.Key).ToList();
            var foundValues = foundKeys.ToDictionary(k => k, k => loaded[k].Value!);
            for (var i = 0; i < group.Key; i++)
            {
                await _layers[i].SetManyAsync(foundValues, ttl, cancellationToken);
            }
        }
    }
}
