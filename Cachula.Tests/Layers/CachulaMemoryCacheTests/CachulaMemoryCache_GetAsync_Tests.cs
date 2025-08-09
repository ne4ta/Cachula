using Cachula.Layers;
using Microsoft.Extensions.Caching.Memory;

namespace Cachula.Tests.Layers.CachulaMemoryCacheTests;

[TestFixture]
public class CachulaMemoryCache_GetAsync_Tests
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
    public async Task ReturnsEntry_WhenValueExistsAndTypeMatches()
    {
        var key = "foo";
        var value = 123;
        _memoryCache.TryGetValue(key, out Arg.Any<object?>())
            .Returns(x => { x[1] = value; return true; });

        var entry = await _cache.GetAsync<int>(key);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Value, Is.EqualTo(123));
            Assert.That(entry.IsNull, Is.False);
        });
    }

    [Test]
    public async Task ReturnsMissed_WhenKeyNotFound()
    {
        var key = "bar";
        _memoryCache.TryGetValue(key, out Arg.Any<object?>()).Returns(false);

        var entry = await _cache.GetAsync<int?>(key);

        Assert.That(entry.IsMissed, Is.True);
    }

    [Test]
    public async Task ReturnsMissed_WhenTypeMismatch()
    {
        var key = "baz";
        object value = "not an int";
        _memoryCache.TryGetValue(key, out Arg.Any<object?>())
            .Returns(x => { x[1] = value; return true; });

        var entry = await _cache.GetAsync<int?>(key);

        Assert.That(entry.IsMissed, Is.True);
    }

    [Test]
    public async Task ReturnsEntry_WithNullValue_WhenValueIsNullReference()
    {
        var key = "nullref";
        object? value = null;
        _memoryCache.TryGetValue(key, out Arg.Any<object?>())
            .Returns(x => { x[1] = value; return true; });

        var entry = await _cache.GetAsync<string>(key);

        Assert.Multiple(() =>
        {
            Assert.That(entry.IsNull, Is.True);
            Assert.That(entry.Value, Is.Null);
        });
    }
}
