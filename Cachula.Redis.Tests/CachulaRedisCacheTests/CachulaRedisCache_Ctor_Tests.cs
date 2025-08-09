using System.Text.Json;
using StackExchange.Redis;

namespace Cachula.Redis.Tests.CachulaRedisCacheTests;

public class CachulaRedisCache_Ctor_Tests
{
    [Test]
    public void Ctor_ThrowsArgumentNullException_IfRedisDatabaseIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CachulaRedisCache(null!));
    }

    [Test]
    public async Task Ctor_UsesProvidedSerializerOptions()
    {
        var db = Substitute.For<IDatabase>();
        RedisValue? capturedValue = null;
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Do<RedisValue>(v => capturedValue = v), Arg.Any<TimeSpan>())
            .Returns(Task.FromResult(true));
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        var cache = new CachulaRedisCache(db, options);

        var testObj = new TestClass { MyProperty = "abc", AnotherProperty = null };
        await cache.SetAsync("key", testObj, TimeSpan.FromMinutes(1));
        Assert.That(capturedValue.HasValue, Is.True);
        var json = System.Text.Encoding.UTF8.GetString((byte[])capturedValue!);
        Assert.That(json, Does.Contain("my_property"));
        Assert.That(json, Does.Contain("another_property"));
    }

    [Test]
    public async Task Ctor_UsesDefaultSerializerOptions_IfNotProvided()
    {
        var db = Substitute.For<IDatabase>();
        RedisValue? capturedValue = null;
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Do<RedisValue>(v => capturedValue = v), Arg.Any<TimeSpan>())
            .Returns(Task.FromResult(true));

        var cache = new CachulaRedisCache(db);

        var testObj = new TestClass { MyProperty = "abc", AnotherProperty = null };
        await cache.SetAsync("key", testObj, TimeSpan.FromMinutes(1));
        Assert.That(capturedValue.HasValue, Is.True);
        var json = System.Text.Encoding.UTF8.GetString((byte[])capturedValue!);
        Assert.That(json, Does.Contain("myProperty"));
        Assert.That(json, Does.Not.Contain("anotherProperty"));
    }

    private class TestClass
    {
        public string? MyProperty { get; set; }
        public string? AnotherProperty { get; set; }
    }
}
