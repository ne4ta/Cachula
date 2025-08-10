using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("Cachula.Redis")]

namespace Cachula.Layers;

/// <summary>
/// Wraps a cached value with metadata for expiration and fail-safe handling.
/// </summary>
/// <typeparam name="T">Type of the cached value.</typeparam>
public class CachulaCacheEntry<T>
{
    /// <summary>
    /// Gets a null cache entry.
    /// </summary>
    public static CachulaCacheEntry<T> Null { get; } = new() { IsNull = true };

    /// <summary>
    /// Gets a missed cache entry.
    /// </summary>
    public static CachulaCacheEntry<T> Missed { get; } = new() { IsMissed = true };

    /// <summary>
    /// Gets the cached value.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets the time when the entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets TTL(time to live) for the cached value.
    /// </summary>
    public TimeSpan Ttl { get; init; }

    /// <summary>
    /// Gets absolute expiration time.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset Expiration => CreatedAt.Add(Ttl);

    /// <summary>
    /// Gets a value indicating whether the entry is expired.
    /// </summary>
    [JsonIgnore]
    public bool IsExpired => DateTimeOffset.UtcNow > Expiration;

    /// <summary>
    /// Gets a value indicating whether the entry is null.
    /// </summary>
    public bool IsNull { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entry is missed (not found in cache).
    /// </summary>
    public bool IsMissed { get; init; }

    /// <summary>
    /// Constructor for deserialization purposes.
    /// </summary>
    public CachulaCacheEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachulaCacheEntry{T}"/> class with a value and TTL.
    /// </summary>
    /// <param name="value">The cached value.</param>
    /// <param name="ttl">The time to live for the cached value.</param>
    public CachulaCacheEntry(T? value, TimeSpan ttl)
    {
        IsNull = value is null;
        Value = value;
        CreatedAt = DateTimeOffset.UtcNow;
        Ttl = ttl;
    }
}
