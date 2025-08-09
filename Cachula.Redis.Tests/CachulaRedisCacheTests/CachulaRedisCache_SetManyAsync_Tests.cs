using System.Text.Json;
using System.Text.Json.Serialization;
using Cachula.Configurations;
using StackExchange.Redis;

namespace Cachula.Redis.Tests.CachulaRedisCacheTests;

[TestFixture]
public class CachulaRedisCache_SetManyAsync_Tests
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
    public async Task CallsStringSetAsync_ForAllPairs()
    {
        var values = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
        var batch = Substitute.For<IBatch>();
        _redis.CreateBatch().Returns(batch);
        batch.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>()
        ).Returns(Task.FromResult(true));

        await _cache.SetManyAsync(values, TimeSpan.FromMinutes(1));

        await batch.Received(1).StringSetAsync("a", Arg.Any<RedisValue>(), Arg.Any<TimeSpan>());
        await batch.Received(1).StringSetAsync("b", Arg.Any<RedisValue>(), Arg.Any<TimeSpan>());
        batch.Received(1).Execute();
    }

    [Test]
    public async Task SplitsIntoBatches_WhenMoreThanBatchSize()
    {
        var cache = new CachulaRedisCache(_redis, _options);
        InitBatchSize(cache, batchSize: 3);
        var values = Enumerable.Range(0, 7).ToDictionary(i => $"k{i}", i => i);
        var batch = Substitute.For<IBatch>();
        _redis.CreateBatch().Returns(batch);
        batch.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>()
        ).Returns(Task.FromResult(true));

        await cache.SetManyAsync(values, TimeSpan.FromMinutes(1));

        batch.Received(7).StringSetAsync(Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
        batch.Received(3).Execute();
    }

    [Test]
    public async Task DoesNothing_WhenDictionaryIsEmpty()
    {
        var values = new Dictionary<string, int>();

        await _cache.SetManyAsync(values, TimeSpan.FromMinutes(1));

        _redis.DidNotReceive().CreateBatch();
    }

    [Test]
    public void ThrowsArgumentNullException_WhenValuesIsNull()
    {
        var message = Assert.ThrowsAsync<ArgumentNullException>(
            () => _cache.SetManyAsync<int>(null!, TimeSpan.FromMinutes(1))).Message;
        Assert.That(message, Does.Contain("Parameter 'values'"));
    }

    private static void InitBatchSize(CachulaRedisCache cache, int batchSize)
    {
        typeof(CachulaRedisCache)
            .GetField("_settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(cache, new CachulaRedisCacheSettings { BatchSize = batchSize });
    }
}
