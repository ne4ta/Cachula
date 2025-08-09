using System.Text.Json;
using System.Text.Json.Serialization;
using Cachula.Layers;
using Cachula.Tests;
using StackExchange.Redis;

namespace Cachula.Redis.Tests.CachulaRedisCacheTests;

[TestFixture]
public class CachulaRedisCache_GetAsync_Tests
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
    public async Task ReturnsEntry_WhenValueExists()
    {
        var key = "foo";
        var entry = new CachulaCacheEntry<int>(123, TimeSpan.FromMinutes(1));
        var redisValue = ToRedisValue(entry);
        _redis.StringGetAsync(key).Returns(redisValue);

        var result = await _cache.GetAsync<int>(key);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(123));
            Assert.That(result.IsNull, Is.False);
        });
    }

    [Test]
    public async Task ReturnsMissed_WhenKeyNotFound()
    {
        var key = "bar";
        _redis.StringGetAsync(key).Returns(RedisValue.Null);

        var result = await _cache.GetAsync<int>(key);
        Assert.That(result.IsMissed, Is.True);
    }

    [Test]
    public async Task ReturnsNull_WhenValueIsNullReference()
    {
        var key = "nullref";
        var entry = CachulaCacheEntry<string>.Null;
        var redisValue = ToRedisValue(entry);
        _redis.StringGetAsync(key).Returns(redisValue);

        var result = await _cache.GetAsync<string>(key);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsNull, Is.True);
            Assert.That(result.Value, Is.Null);
        });
    }

    [Test]
    public async Task ReturnsEntry_WhenComplexObjectExists()
    {
        var key = "obj";
        var entry = new CachulaCacheEntry<DummyObj>(new DummyObj { Id = 42, Name = "test" }, TimeSpan.FromMinutes(1));
        var redisValue = ToRedisValue(entry);
        _redis.StringGetAsync(key).Returns(redisValue);

        var result = await _cache.GetAsync<DummyObj>(key);
        Assert.That(result.Value, Is.EqualTo(entry.Value));
    }

    [Test]
    public void ThrowsArgumentNullException_WhenKeyIsNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetAsync(null!, 1, TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task ReturnsMissed_WhenDeserializationFails()
    {
        var key = "badjson";
        var invalidJson = "{ invalid json }"u8.ToArray();
        _redis.StringGetAsync(key).Returns((RedisValue)invalidJson);

        var result = await _cache.GetAsync<int>(key);

        Assert.That(result.IsMissed, Is.True);
    }

    private RedisValue ToRedisValue<T>(CachulaCacheEntry<T> entry)
    {
        return JsonSerializer.SerializeToUtf8Bytes(entry, _options);
    }
}
