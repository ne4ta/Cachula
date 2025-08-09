using Cachula.Layers;
using Microsoft.Extensions.Caching.Memory;

namespace Cachula.Tests.Layers.CachulaMemoryCacheTests;

[TestFixture]
public class CachulaMemoryCache_SetAsync_Tests
{
    private IMemoryCache _memoryCache = null!;
    private CachulaMemoryCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _memoryCache = Substitute.For<IMemoryCache>();
        _cache = new CachulaMemoryCache(_memoryCache);
    }

    [Test]
    public async Task SetsValueWithCorrectExpiration()
    {
        var key = "foo";
        var value = 123;
        var expiration = TimeSpan.FromMinutes(10);
        var cacheEntry = Substitute.For<ICacheEntry>();
        _memoryCache.CreateEntry(key).Returns(cacheEntry);

        await _cache.SetAsync(key, value, expiration);

        _memoryCache.Received(1).CreateEntry(key);
        cacheEntry.Received(1).Value = value;
        Assert.That(cacheEntry.AbsoluteExpirationRelativeToNow, Is.EqualTo(expiration));
    }

    [Test]
    public async Task AllowsNullValue_ForReferenceType()
    {
        var key = "foo";
        string? value = null;
        var expiration = TimeSpan.FromMinutes(5);
        var cacheEntry = Substitute.For<ICacheEntry>();
        _memoryCache.CreateEntry(key).Returns(cacheEntry);

        await _cache.SetAsync(key, value, expiration);

        _memoryCache.Received(1).CreateEntry(key);
        cacheEntry.Received(1).Value = value;
    }

    [Test]
    public async Task DoesNotThrow_WhenCalledConcurrently()
    {
        var key = "foo";
        var expiration = TimeSpan.FromMinutes(1);
        var cacheEntry = Substitute.For<ICacheEntry>();
        _memoryCache.CreateEntry(key).Returns(cacheEntry);
        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            var value = i;
            tasks[i] = _cache.SetAsync(key, value, expiration);
        }

        await Task.WhenAll(tasks);
        _memoryCache.Received(10).CreateEntry(key);
    }
}
