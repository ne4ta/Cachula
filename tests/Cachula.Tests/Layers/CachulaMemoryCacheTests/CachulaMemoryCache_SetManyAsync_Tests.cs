using Cachula.Layers;
using Microsoft.Extensions.Caching.Memory;

namespace Cachula.Tests.Layers.CachulaMemoryCacheTests;

[TestFixture]
public class CachulaMemoryCache_SetManyAsync_Tests
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
    public async Task SetsAllValuesWithCorrectExpiration()
    {
        var values = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
        var ttl = TimeSpan.FromMinutes(7);
        var entries = new Dictionary<string, ICacheEntry>();
        foreach (var key in values.Keys)
        {
            var entry = Substitute.For<ICacheEntry>();
            entries[key] = entry;
            _memoryCache.CreateEntry(key).Returns(entry);
        }

        await _cache.SetManyAsync(values, ttl);

        foreach (var kvp in values)
        {
            _memoryCache.Received(1).CreateEntry(kvp.Key);
            entries[kvp.Key].Received(1).Value = kvp.Value;
            Assert.That(entries[kvp.Key].AbsoluteExpirationRelativeToNow, Is.EqualTo(ttl));
        }
    }

    [Test]
    public async Task AllowsNullValues_ForReferenceType()
    {
        var values = new Dictionary<string, string?> { ["a"] = null, ["b"] = "foo" };
        var ttl = TimeSpan.FromMinutes(2);
        var entries = new Dictionary<string, ICacheEntry>();
        foreach (var key in values.Keys)
        {
            var entry = Substitute.For<ICacheEntry>();
            entries[key] = entry;
            _memoryCache.CreateEntry(key).Returns(entry);
        }

        await _cache.SetManyAsync(values, ttl);

        _memoryCache.Received(1).CreateEntry("a");
        _memoryCache.Received(1).CreateEntry("b");
        entries["a"].Received(1).Value = null;
        entries["b"].Received(1).Value = "foo";
    }

    [Test]
    public async Task DoesNothing_WhenEmptyDictionary()
    {
        var values = new Dictionary<string, int>();
        var ttl = TimeSpan.FromMinutes(1);

        await _cache.SetManyAsync(values, ttl);

        _memoryCache.DidNotReceive().CreateEntry(Arg.Any<string>());
    }
}
