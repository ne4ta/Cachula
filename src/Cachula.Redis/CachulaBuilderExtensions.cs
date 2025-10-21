using System.Text.Json;
using Cachula.Configurations;
using StackExchange.Redis;

namespace Cachula.Redis;

/// <summary>
/// Extension methods for integrating Redis cache with CachulaBuilder.
/// </summary>
public static class CachulaBuilderExtensions
{
    /// <summary>
    /// Adds a Redis cache layer to the Cachula cache builder.
    /// </summary>
    /// <param name="builder">The Cachula cache builder.</param>
    /// <param name="db">The Redis database instance.</param>
    /// <param name="settings">Optional settings for Cachula Redis cache (e.g., BatchSize).</param>
    /// <param name="json">Optional serializer options for JSON serialization.</param>
    /// <returns>The updated <see cref="CachulaBuilder"/> instance.</returns>
    public static CachulaBuilder WithRedis(
        this CachulaBuilder builder,
        IDatabase db,
        CachulaRedisCacheSettings? settings = null,
        JsonSerializerOptions? json = null)
    {
        var redisCacheLayer = new CachulaRedisCache(db, json, settings);
        builder.WithLayer(redisCacheLayer);
        return builder;
    }
}
