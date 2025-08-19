namespace Cachula.Configurations;

/// <summary>
/// Represents settings for the Cachula Redis cache.
/// </summary>
public class CachulaRedisCacheSettings
{
    /// <summary>
    /// Gets the batch size for Redis cache operations.
    /// </summary>
    public int BatchSize { get; init; } = 100;
}
