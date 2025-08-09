using Cachula.Layers;
using Microsoft.Extensions.Caching.Memory;

namespace Cachula.Tests.Layers.CachulaMemoryCacheTests;

[TestFixture]
public class CachulaMemoryCache_RemoveManyAsync_Tests
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
    public async Task RemovesAllKeys()
    {
        var keys = new[] { "a", "b", "c" };

        await _cache.RemoveManyAsync(keys);

        foreach (var key in keys)
        {
            _memoryCache.Received(1).Remove(key);
        }
    }

    [Test]
    public async Task DoesNothing_WhenEmpty()
    {
        var keys = Array.Empty<string>();

        await _cache.RemoveManyAsync(keys);

        _memoryCache.DidNotReceive().Remove(Arg.Any<string>());
    }

    [Test]
    public async Task AllowsDuplicates_ButRemovesEachOnce()
    {
        var keys = new[] { "a", "b", "a", "c", "b" };

        await _cache.RemoveManyAsync(keys);

        Assert.That(
            _memoryCache.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Remove"),
            Is.EqualTo(keys.Length));
    }
}
