using Cachula.Layers;

namespace Cachula.Tests.Layers;

[TestFixture]
public class NullCacheLayerTests
{
    private NullCacheLayer _layer = null!;

    [SetUp]
    public void SetUp()
    {
        _layer = new NullCacheLayer();
    }

    [Test]
    public async Task GetAsync_AlwaysReturnsMissedEntry()
    {
        var result = await _layer.GetAsync<string>("any-key");

        Assert.That(result.IsMissed, Is.True);
    }

    [Test]
    public async Task GetManyAsync_AlwaysReturnsMissedEntries()
    {
        var keys = new[] { "a", "b", "c" };

        var result = await _layer.GetManyAsync<string>(keys);

        Assert.Multiple(() =>
        {
            Assert.That(result.Keys, Is.EquivalentTo(keys));
            Assert.That(result.Values.All(e => e.IsMissed), Is.True);
        });
    }

    [Test]
    public void SetAsync_DoesNothing()
    {
        Assert.DoesNotThrowAsync(() => _layer.SetAsync("key", "value", TimeSpan.FromMinutes(1)));
    }

    [Test]
    public void SetManyAsync_DoesNothing()
    {
        var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };

        Assert.DoesNotThrowAsync(() => _layer.SetManyAsync(dict, TimeSpan.FromMinutes(1)));
    }

    [Test]
    public void RemoveManyAsync_DoesNothing()
    {
        var keys = new[] { "a", "b" };

        Assert.DoesNotThrowAsync(() => _layer.RemoveManyAsync(keys));
    }
}
