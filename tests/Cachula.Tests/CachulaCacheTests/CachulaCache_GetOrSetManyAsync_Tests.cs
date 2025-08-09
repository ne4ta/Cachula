using Cachula.Interfaces;
using Cachula.Layers;

namespace Cachula.Tests.CachulaCacheTests;

[TestFixture]
public class CachulaCache_GetOrSetManyAsync_Tests
{
    private ICachulaEngine _engine;
    private CachulaCache _cache;

    [SetUp]
    public void SetUp()
    {
        _engine = Substitute.For<ICachulaEngine>();
        _cache = new CachulaCache(_engine);
    }

    [Test]
    public async Task Returns_All_Values_If_Cache_Hit()
    {
        var keys = new[] { "a", "b" };
        var entries = new Dictionary<string, CachulaCacheEntry<int>>
        {
            { "a", new CachulaCacheEntry<int>(1, TimeSpan.FromMinutes(1)) },
            { "b", new CachulaCacheEntry<int>(2, TimeSpan.FromMinutes(1)) }
        };
        _engine.GetOrSetManyAsync(
            keys,
            Arg.Any<Func<IEnumerable<string>, Task<IDictionary<string, int>>>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(entries);

        var result = await _cache.GetOrSetManyAsync(
            keys,
            (_, _) => Task.FromResult<IDictionary<string, int>>(new Dictionary<string, int>()),
            TimeSpan.FromMinutes(1));

        Assert.That(result, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task Returns_Only_Found_Values_If_Some_Null()
    {
        var keys = new[] { "a", "b", "c" };
        var entries = new Dictionary<string, CachulaCacheEntry<int>>
        {
            { "a", new CachulaCacheEntry<int>(1, TimeSpan.FromMinutes(1)) },
            { "b", CachulaCacheEntry<int>.Null },
            { "c", new CachulaCacheEntry<int>(3, TimeSpan.FromMinutes(1)) }
        };
        _engine.GetOrSetManyAsync(
            keys,
            Arg.Any<Func<IEnumerable<string>, Task<IDictionary<string, int>>>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(entries);

        var result = await _cache.GetOrSetManyAsync(
            keys,
            (_, _) => Task.FromResult<IDictionary<string, int>>(new Dictionary<string, int>()),
            TimeSpan.FromMinutes(1));

        Assert.That(result, Is.EquivalentTo(new[] { 1, 3 }));
    }

    [Test]
    public async Task Passes_CancellationToken_To_Factory()
    {
        var keys = new[] { "a", "b" };
        var token = new CancellationTokenSource().Token;
        var called = false;
        _engine.GetOrSetManyAsync(
            keys,
            Arg.Any<Func<IEnumerable<string>, Task<IDictionary<string, int>>>>(),
            Arg.Any<TimeSpan>(),
            token)
            .Returns(callInfo =>
            {
                var func = (Func<IEnumerable<string>, Task<IDictionary<string, int>>>)callInfo.Args()[1];
                func(["a"]);
                return new Dictionary<string, CachulaCacheEntry<int>>
                {
                    { "a", new CachulaCacheEntry<int>(1, TimeSpan.FromMinutes(1)) },
                    { "b", new CachulaCacheEntry<int>(2, TimeSpan.FromMinutes(1)) }
                };
            });

        await _cache.GetOrSetManyAsync(
            keys,
            (_, ct) =>
            {
                called = ct == token;
                return Task.FromResult<IDictionary<string, int>>(new Dictionary<string, int>());
            },
            TimeSpan.FromMinutes(1),
            token);

        Assert.That(called, Is.True);
    }
}
