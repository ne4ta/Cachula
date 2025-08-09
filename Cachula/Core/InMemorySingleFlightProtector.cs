using System.Collections.Concurrent;
using Cachula.Interfaces;

namespace Cachula.Core;

/// <summary>
/// In-memory implementation of <see cref="IStampedeProtector"/>.
/// Prevents cache stampede by ensuring that only one loader runs per key at a time in the current process.
/// Uses a concurrent dictionary and lazy initialization to deduplicate concurrent requests for the same key.
/// </summary>
public class InMemorySingleFlightProtector : IStampedeProtector
{
    private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _inflight = new();

    /// <inheritdoc />
    public async Task<T> RunAsync<T>(string key, Func<Task<T>> loader)
    {
        var lazyTask = _inflight.GetOrAdd(key, _ => new Lazy<Task<object>>(async () =>
        {
            try
            {
                var value = await loader().ConfigureAwait(false);
                return new Dictionary<string, object?> { [key] = value };
            }
            finally
            {
                _inflight.TryRemove(key, out var _);
            }
        }));

        var dict = (IDictionary<string, object>)await lazyTask.Value.ConfigureAwait(false);
        return (T)dict[key];
    }

    /// <inheritdoc />
    public IDictionary<string, Task<T>> RunManyAsync<T>(
        IEnumerable<string> keys,
        Func<IEnumerable<string>, Task<IDictionary<string, T>>> loader)
    {
        var result = new Dictionary<string, Task<T?>>();
        var toLoad = new List<string>();

        foreach (var key in keys)
        {
            if (_inflight.TryGetValue(key, out var lazyTask))
            {
                result[key] = GetBatchResultAsync<T>(lazyTask.Value, key);
            }
            else
            {
                toLoad.Add(key);
            }
        }

        if (toLoad.Count > 0)
        {
            var lazy = new Lazy<Task<object>>(ValueFactory, true);

            async Task<object> ValueFactory()
            {
                try
                {
                    var loaded = await loader(toLoad).ConfigureAwait(false);
                    return loaded.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
                }
                finally
                {
                    foreach (var key in toLoad)
                    {
                        _inflight.TryRemove(key, out _);
                    }
                }
            }

            foreach (var key in toLoad)
            {
                _inflight[key] = lazy;
            }

            var batchTask = lazy.Value;
            foreach (var key in toLoad)
            {
                result[key] = GetBatchResultAsync<T>(batchTask, key);
            }
        }

        return result;
    }

    private async Task<T?> GetBatchResultAsync<T>(Task<object> batchTask, string key)
    {
        var dict = (IDictionary<string, object>)await batchTask.ConfigureAwait(false);
        return dict.TryGetValue(key, out var value) ? (T)value : default;
    }
}
