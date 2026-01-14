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
    private readonly CachulaRedisCacheSettings _settings;
    
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //// Ignore null values during serialization.
        //// This helps reduce payload size and avoids unnecessary data storage.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CachulaRedisCache"/> class.
    /// </summary>
    /// <param name="redisDatabase">The Redis database instance.</param>
    /// <param name="serializerOptions">Optional serializer options for JSON serialization.</param>
    /// <param name="settings">Optional settings for Cachula Redis cache (e.g., BatchSize).</param>
    /// <remarks>
    /// This constructor requires an instance of <see cref="IDatabase"/> from StackExchange.Redis.
    /// </remarks>
    public CachulaRedisCache(IDatabase redisDatabase, JsonSerializerOptions? serializerOptions = null, CachulaRedisCacheSettings? settings = null)
    {
        _redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
        _serializerOptions = serializerOptions ?? DefaultSerializerOptions;
        _settings = settings ?? new CachulaRedisCacheSettings();
        if (_settings.BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "BatchSize must be greater than zero.");
        }
    }

    /// <inheritdoc />
    public async Task<CachulaCacheEntry<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var value = await _redisDatabase.StringGetAsync(key).ConfigureAwait(false);
        return FromRedisValue<T>(value);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var redisValue = ToRedisValue(value, expiration);
        return _redisDatabase.StringSetAsync(key, redisValue, expiration);
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, CachulaCacheEntry<T>>> GetManyAsync<T>(
        IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }
        
        var keyArray = keys as string[] ?? keys.ToArray();
        if (keyArray.Length == 0)
        {
            return new Dictionary<string, CachulaCacheEntry<T>>(0, StringComparer.Ordinal);
        }

        var result = new Dictionary<string, CachulaCacheEntry<T>>(keyArray.Length, StringComparer.Ordinal);

        foreach (var keyChunk in keyArray.Chunk(_settings.BatchSize))
        {
            var redisKeys = new RedisKey[keyChunk.Length];
            for (var i = 0; i < keyChunk.Length; i++)
            {
                redisKeys[i] = keyChunk[i];
            }

            var values = await _redisDatabase.StringGetAsync(redisKeys).ConfigureAwait(false);

            for (var i = 0; i < keyChunk.Length; i++)
            {
                result[keyChunk[i]] = FromRedisValue<T>(values[i]);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task SetManyAsync<T>(
        IDictionary<string, T> values, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        foreach (var keyValuePairs in values.Chunk(_settings.BatchSize))
        {
            var batch = _redisDatabase.CreateBatch();

            var tasks = keyValuePairs
                .Select(kvp => batch.StringSetAsync(kvp.Key, ToRedisValue(kvp.Value, ttl), ttl))
                .ToList(); // ToList() is important here to execute the Select. It's important for the tasks to be created BEFORE batch.Execute().

            batch.Execute();
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        var arr = keys as string[] ?? keys.ToArray();
        if (arr.Length == 0)
        {
            return;
        }

        var redisKeys = Array.ConvertAll(arr, k => (RedisKey)k);
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
