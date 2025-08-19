using Cachula.Configurations;
using StackExchange.Redis;

namespace Cachula.Redis.Tests.CachulaRedisCacheTests;

[TestFixture]
public class CachulaRedisCache_BatchSize_Tests
{
    [Test]
    public void Ctor_ThrowsArgumentOutOfRange_WhenBatchSizeIsZeroOrNegative()
    {
        var db = Substitute.For<IDatabase>();

        Assert.Throws<ArgumentOutOfRangeException>(() => new CachulaRedisCache(db, settings: new CachulaRedisCacheSettings { BatchSize = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CachulaRedisCache(db, settings: new CachulaRedisCacheSettings { BatchSize = -5 }));
    }

    [Test]
    public async Task SetManyAsync_SplitsIntoBatches_BySettingsBatchSize()
    {
        // Arrange
        var db = Substitute.For<IDatabase>();
        var settings = new CachulaRedisCacheSettings { BatchSize = 2 };
        var executeCalls = 0;
        var createBatchCalls = 0;
        var batches = new List<IBatch>();

        db.CreateBatch().Returns(ci =>
        {
            createBatchCalls++;
            var batch = Substitute.For<IBatch>();
            batches.Add(batch);

            batch.StringSetAsync(
                    Arg.Any<RedisKey>(),
                    Arg.Any<RedisValue>(),
                    Arg.Any<TimeSpan?>(),
                    Arg.Any<When>(),
                    Arg.Any<CommandFlags>())
                .Returns(Task.FromResult(true));

            batch.When(b => b.Execute()).Do(_ => executeCalls++);

            return batch;
        });

        var cache = new CachulaRedisCache(db, settings: settings);
        var values = new Dictionary<string, string>
        {
            ["k1"] = "v1",
            ["k2"] = "v2",
            ["k3"] = "v3",
            ["k4"] = "v4",
            ["k5"] = "v5",
        };

        // Act
        await cache.SetManyAsync(values, TimeSpan.FromMinutes(1));
        
        // Assert
        Assert.Multiple(() =>
        {
            // With BatchSize=2 and 5 items, we expect 3 batches (2+2+1)
            Assert.That(createBatchCalls, Is.EqualTo(3));
            Assert.That(executeCalls, Is.EqualTo(3));
        });

        var stringSetCallsTotal = batches
            .SelectMany(b => b.ReceivedCalls())
            .Count(ci => ci.GetMethodInfo().Name == nameof(IBatch.StringSetAsync));
        Assert.That(stringSetCallsTotal, Is.EqualTo(5));
    }
}
