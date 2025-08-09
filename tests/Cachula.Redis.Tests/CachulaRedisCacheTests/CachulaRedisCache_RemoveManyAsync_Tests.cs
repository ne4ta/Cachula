using StackExchange.Redis;

namespace Cachula.Redis.Tests.CachulaRedisCacheTests;

[TestFixture]
public class CachulaRedisCache_RemoveManyAsync_Tests
{
    private IDatabase _redis = null!;
    private CachulaRedisCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _redis = Substitute.For<IDatabase>();
        _cache = new CachulaRedisCache(_redis);
    }

    [Test]
    public async Task CallsKeyDeleteAsync_WithCorrectKeys()
    {
        var keys = new[] { "a", "b", "c" };
        RedisKey[]? capturedKeys = null;
        _redis.KeyDeleteAsync(Arg.Do<RedisKey[]>(rk => capturedKeys = rk)).Returns(3);

        await _cache.RemoveManyAsync(keys);

        Assert.That(capturedKeys, Is.Not.Null);
        Assert.That(capturedKeys!.Select(k => (string)k), Is.EquivalentTo(keys));
    }

    [Test]
    public void ThrowsArgumentNullException_WhenKeysIsNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _cache.RemoveManyAsync(null!));
    }
}
