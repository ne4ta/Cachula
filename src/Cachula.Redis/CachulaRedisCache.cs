using System.Text.Json;
using System.Text.Json.Serialization;
using Cachula.Configurations;
using Cachula.Interfaces;
using Cachula.Layers;
using StackExchange.Redis;

namespace Cachula.Redis;

/// <summary>
/// A distributed cache implementation using Redis.
/// </summary>
public class CachulaRedisCache : ICachulaDistributedCache
{
    private readonly IDatabase _redisDatabase;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly CachulaRedisCacheSettings _settings = new()
    {
        // TODO: Make it configurable via DI or options pattern.
        BatchSize = 100,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CachulaRedisCache"/> class.
    /// </summary>
    /// <param name="redisDatabase">The Redis database instance.</param>
    /// <param name="serializerOptions">Optional serializer options for JSON serialization.</param>
    /// <remarks>
    /// This constructor requires an instance of <see cref="IDatabase"/> from StackExchange.Redis.
    /// </remarks>
    public CachulaRedisCache(IDatabase redisDatabase, JsonSerializerOptions? serializerOptions = null)
    {
        _redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //// Ignore null values during serialization.
            //// This helps reduce payload size and avoids unnecessary data storage.
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    /// <inheritdoc />
    public async Task<CachulaCacheEntry<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var value = await _redisDatabase.StringGetAsync(key);
        return FromRedisValue<T>(value);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var redisValue = ToRedisValue(value, expiration);
        return _redisDatabase.StringSetAsync(key, redisValue, expiration);
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, CachulaCacheEntry<T>>> GetManyAsync<T>(
        IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
        var values = await _redisDatabase.StringGetAsync(redisKeys);
        var result = new Dictionary<string, CachulaCacheEntry<T>>();
        for (var i = 0; i < redisKeys.Length; i++)
        {
            var value = values[i];
            result[redisKeys[i]!] = FromRedisValue<T>(value);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task SetManyAsync<T>(
        IDictionary<string, T> values, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (var keyValuePairs in values.Chunk(_settings.BatchSize))
        {
            var batch = _redisDatabase.CreateBatch();

            var tasks = keyValuePairs
                .Select(kvp => batch.StringSetAsync(kvp.Key, ToRedisValue(kvp.Value, ttl), ttl))
                .ToList(); // ToList() is important here to execute the Select. It's important for the tasks to be created BEFORE batch.Execute().

            batch.Execute();
            await Task.WhenAll(tasks);
        }
    }

    /// <inheritdoc />
    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
        await _redisDatabase.KeyDeleteAsync(redisKeys).ConfigureAwait(false);
    }

    private RedisValue ToRedisValue<T>(T value, TimeSpan ttl)
    {
        var entry = new CachulaCacheEntry<T>(value, ttl);
        return JsonSerializer.SerializeToUtf8Bytes(entry, _serializerOptions);
    }

    private CachulaCacheEntry<T> FromRedisValue<T>(RedisValue value)
    {
        if (!value.HasValue)
        {
            return CachulaCacheEntry<T>.Missed;
        }

        try
        {
            var entry = JsonSerializer.Deserialize<CachulaCacheEntry<T>>(value!, _serializerOptions);
            return entry ?? CachulaCacheEntry<T>.Null;
        }
        catch (JsonException)
        {
            return CachulaCacheEntry<T>.Missed;
        }
    }
}
