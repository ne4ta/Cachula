using Cachula.Interfaces;
using Cachula.Layers;

namespace Cachula.Tests.CachulaCacheTests;

[TestFixture]
public class CachulaCache_GetOrSetAsync_Tests
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
    public async Task Returns_Value_If_Cache_Hit()
    {
        var expected = 42;
        _engine.GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<int>>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(new CachulaCacheEntry<int>(expected, TimeSpan.FromMinutes(1)));

        var result = await _cache.GetOrSetAsync(
            "key",
            _ => Task.FromResult(100),
            TimeSpan.FromMinutes(1));

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task Returns_Default_If_Cache_Miss()
    {
        _engine.GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<string?>>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(CachulaCacheEntry<string>.Missed);

        var result = await _cache.GetOrSetAsync<string>(
            "key",
            _ => Task.FromResult<string?>(null),
            TimeSpan.FromMinutes(1));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Passes_CancellationToken_To_Factory()
    {
        var token = new CancellationTokenSource().Token;
        var called = false;
        _engine.GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<int>>>(),
            Arg.Any<TimeSpan>(),
            token)
            .Returns(callInfo =>
            {
                var func = (Func<Task<int>>)callInfo.Args()[1];
                return new CachulaCacheEntry<int>(func().Result, TimeSpan.FromMinutes(1));
            });

        await _cache.GetOrSetAsync(
            "key",
            ct => { called = ct == token; return Task.FromResult(1); },
            TimeSpan.FromMinutes(1),
            token);

        Assert.That(called, Is.True);
    }
}
