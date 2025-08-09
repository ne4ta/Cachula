using System.Text.Json;
using System.Text.Json.Serialization;
using Cachula.Layers;
using Cachula.Tests;
using StackExchange.Redis;

namespace Cachula.Redis.Tests.CachulaRedisCacheTests;

[TestFixture]
public class CachulaRedisCache_SetAsync_Tests
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
    public async Task CallsStringSetAsync_WithCorrectParameters()
    {
        var key = "foo";
        var value = 123;
        var expiration = TimeSpan.FromMinutes(5);
        RedisValue? capturedValue = null;
        TimeSpan? capturedExpiry = null;
        _redis.StringSetAsync(key, Arg.Do<RedisValue>(v => capturedValue = v), Arg.Do<TimeSpan>(e => capturedExpiry = e))
            .Returns(true);

        await _cache.SetAsync(key, value, expiration);

        Assert.Multiple(() =>
        {
            Assert.That(capturedValue, Is.Not.Null);
            Assert.That(capturedExpiry, Is.EqualTo(expiration));
        });
    }

    [Test]
    public async Task SerializesSimpleValue_Correctly()
    {
        var key = "bar";
        var value = 42;
        var expiration = TimeSpan.FromSeconds(10);
        RedisValue? capturedValue = null;
        _redis.StringSetAsync(key, Arg.Do<RedisValue>(v => capturedValue = v), expiration)
            .Returns(true);

        await _cache.SetAsync(key, value, expiration);

        var entry = JsonSerializer.Deserialize<CachulaCacheEntry<int>>(capturedValue, _options);
        Assert.That(entry!.Value, Is.EqualTo(value));
    }

    [Test]
    public async Task SerializesComplexObject_Correctly()
    {
        var key = "obj";
        var value = new DummyObj { Id = 7, Name = "test" };
        var expiration = TimeSpan.FromSeconds(20);
        RedisValue? capturedValue = null;
        _redis.StringSetAsync(key, Arg.Do<RedisValue>(v => capturedValue = v), expiration)
            .Returns(true);

        await _cache.SetAsync(key, value, expiration);

        var entry = JsonSerializer.Deserialize<CachulaCacheEntry<DummyObj>>(capturedValue, _options);
        Assert.That(entry!.Value, Is.EqualTo(value));
    }

    [Test]
    public void ThrowsArgumentNullException_WhenKeyIsNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetAsync(null!, 1, TimeSpan.FromSeconds(1)));
    }
}
