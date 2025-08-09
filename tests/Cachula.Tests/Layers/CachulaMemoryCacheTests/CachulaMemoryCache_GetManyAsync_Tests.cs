using Cachula.Layers;
using Microsoft.Extensions.Caching.Memory;

namespace Cachula.Tests.Layers.CachulaMemoryCacheTests;

[TestFixture]
public class CachulaMemoryCache_GetManyAsync_Tests
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
    public async Task ReturnsEntries_ForAllKeys()
    {
        var keys = new[] { "a", "b", "c" };
        _memoryCache.TryGetValue("a", out Arg.Any<object?>()).Returns(x => { x[1] = 1; return true; });
        _memoryCache.TryGetValue("b", out Arg.Any<object?>()).Returns(x => { x[1] = 2; return true; });
        _memoryCache.TryGetValue("c", out Arg.Any<object?>()).Returns(x => { x[1] = 3; return true; });

        var result = await _cache.GetManyAsync<int>(keys);

        Assert.Multiple(() =>
        {
            Assert.That(result["a"].Value, Is.EqualTo(1));
            Assert.That(result["b"].Value, Is.EqualTo(2));
            Assert.That(result["c"].Value, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task ReturnsNull_WhenKeyNotFound()
    {
        var keys = new[] { "a", "b" };
        _memoryCache.TryGetValue("a", out Arg.Any<object?>()).Returns(false);
        _memoryCache.TryGetValue("b", out Arg.Any<object?>()).Returns(x => { x[1] = 42; return true; });

        var result = await _cache.GetManyAsync<int>(keys);

        Assert.Multiple(() =>
        {
            Assert.That(result["a"].IsMissed, Is.True);
            Assert.That(result["b"].Value, Is.EqualTo(42));
        });
    }

    [Test]
    public async Task ReturnsNull_WhenTypeMismatch()
    {
        var keys = new[] { "a", "b" };
        _memoryCache.TryGetValue("a", out Arg.Any<object?>()).Returns(x => { x[1] = "not an int"; return true; });
        _memoryCache.TryGetValue("b", out Arg.Any<object?>()).Returns(x => { x[1] = 99; return true; });

        var result = await _cache.GetManyAsync<int>(keys);
        Assert.Multiple(() =>
        {
            Assert.That(result["a"].IsMissed, Is.True);
            Assert.That(result["b"].Value, Is.EqualTo(99));
        });
    }

    [Test]
    public async Task ReturnsEntry_WithNullValue_WhenValueIsNullReference()
    {
        var keys = new[] { "a" };
        object? value = null;
        _memoryCache.TryGetValue("a", out Arg.Any<object?>()).Returns(x => { x[1] = value; return true; });

        var result = await _cache.GetManyAsync<string>(keys);

        Assert.Multiple(() =>
        {
            Assert.That(result["a"].IsNull, Is.True);
            Assert.That(result["a"].Value, Is.Null);
        });
    }
}
