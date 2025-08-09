using System.Text.Json;
using System.Text.Json.Serialization;
using Cachula.Layers;
using Cachula.Tests;
using StackExchange.Redis;

namespace Cachula.Redis.Tests.CachulaRedisCacheTests;

[TestFixture]
public class CachulaRedisCache_GetManyAsync_Tests
{
    private IDatabase _redis = null!;
    private CachulaRedisCache _cache = null!;
    private JsonSerializerOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _redis = Substitute.For<IDatabase>();
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _cache = new CachulaRedisCache(_redis, _options);
    }

    [Test]
    public async Task ReturnsEntries_ForAllKeys()
    {
        var keys = new[] { "a", "b", "c" };
        var entries = new[]
        {
            new CachulaCacheEntry<int>(1, TimeSpan.FromMinutes(1)),
            new CachulaCacheEntry<int>(2, TimeSpan.FromMinutes(1)),
            new CachulaCacheEntry<int>(3, TimeSpan.FromMinutes(1))
        };
        var redisValues = entries.Select(ToRedisValue).ToArray();
        _redis.StringGetAsync(Arg.Is<RedisKey[]>(x => x.SequenceEqual(keys.Select(k => (RedisKey)k))))
            .Returns(redisValues);

        var result = await _cache.GetManyAsync<int>(keys);

        Assert.Multiple(() =>
        {
            Assert.That(result.Keys, Is.EquivalentTo(keys));
            Assert.That(result["a"].Value, Is.EqualTo(1));
            Assert.That(result["b"].Value, Is.EqualTo(2));
            Assert.That(result["c"].Value, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task ReturnsMissed_ForMissingKeys()
    {
        var keys = new[] { "x", "y" };
        var redisValues = new[] { RedisValue.Null, RedisValue.Null };
        _redis.StringGetAsync(Arg.Any<RedisKey[]>()).Returns(redisValues);

        var result = await _cache.GetManyAsync<int>(keys);

        Assert.Multiple(() =>
        {
            Assert.That(result["x"].IsMissed, Is.True);
            Assert.That(result["y"].IsMissed, Is.True);
        });
    }

    [Test]
    public async Task ReturnsNull_ForNullReferenceValues()
    {
        var keys = new[] { "n" };
        var entry = CachulaCacheEntry<string>.Null;
        var redisValues = new[] { ToRedisValue(entry) };
        _redis.StringGetAsync(Arg.Any<RedisKey[]>()).Returns(redisValues);

        var result = await _cache.GetManyAsync<string>(keys);

        Assert.Multiple(() =>
        {
            Assert.That(result["n"].IsNull, Is.True);
            Assert.That(result["n"].Value, Is.Null);
        });
    }

    [Test]
    public async Task ReturnsComplexObjects_Correctly()
    {
        var keys = new[] { "obj1", "obj2" };
        var entries = new[]
        {
            new CachulaCacheEntry<DummyObj>(new DummyObj { Id = 1, Name = "A" }, TimeSpan.FromMinutes(1)),
            new CachulaCacheEntry<DummyObj>(new DummyObj { Id = 2, Name = "B" }, TimeSpan.FromMinutes(1))
        };
        var redisValues = entries.Select(ToRedisValue).ToArray();
        _redis.StringGetAsync(Arg.Any<RedisKey[]>()).Returns(redisValues);

        var result = await _cache.GetManyAsync<DummyObj>(keys);

        Assert.Multiple(() =>
        {
            Assert.That(result["obj1"].Value, Is.EqualTo(entries[0].Value));
            Assert.That(result["obj2"].Value, Is.EqualTo(entries[1].Value));
        });
    }

    [Test]
    public async Task ReturnsEmptyDictionary_WhenKeysCollectionIsEmpty()
    {
        var keys = Array.Empty<string>();
        _redis.StringGetAsync(Arg.Any<RedisKey[]>()).Returns([]);

        var result = await _cache.GetManyAsync<int>(keys);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ReturnsMissed_WhenDeserializationFails()
    {
        var keys = new[] { "bad1", "good" };
        var invalidJson = "{ invalid json }"u8.ToArray();
        var validEntry = new CachulaCacheEntry<int>(42, TimeSpan.FromMinutes(1));
        var validJson = ToRedisValue(validEntry);
        var redisValues = new[] { (RedisValue)invalidJson, validJson };
        _redis.StringGetAsync(Arg.Is<RedisKey[]>(x => x.SequenceEqual(keys.Select(k => (RedisKey)k))))
            .Returns(redisValues);

        var result = await _cache.GetManyAsync<int>(keys);

        Assert.Multiple(() =>
        {
            Assert.That(result["bad1"].IsMissed, Is.True);
            Assert.That(result["good"].IsMissed, Is.False);
            Assert.That(result["good"].Value, Is.EqualTo(42));
        });
    }

    private RedisValue ToRedisValue<T>(CachulaCacheEntry<T> entry)
    {
        return JsonSerializer.SerializeToUtf8Bytes(entry, _options);
    }
}
