using Cachula.Core;
using Cachula.Interfaces;
using Cachula.Layers;
using Cachula.Tests.Helpers;

namespace Cachula.Tests.Core.CachulaEngineTests;

[TestFixture]
public class CachulaEngine_GetOrSetAsync_Tests
{
    private ICacheLayer _mem;
    private ICacheLayer _redis;
    private IStampedeProtector _protector;
    private CachulaEngine _engine;

    [SetUp]
    public void SetUp()
    {
        _mem = Substitute.For<ICacheLayer>();
        _redis = Substitute.For<ICacheLayer>();
        _protector = Substitute.For<IStampedeProtector>();
        _engine = new CachulaEngine([_mem, _redis], _protector);
    }

    [Test]
    public async Task Returns_FromFirstLayer_IfPresent()
    {
        _mem.GetAsync<int>("k").Returns(new CachulaCacheEntry<int>(42, TimeSpan.Zero));

        var result = await _engine.GetOrSetAsync("k", () => Task.FromResult(100), TimeSpan.FromMinutes(1));

        Assert.That(result.Value, Is.EqualTo(42));
        await _redis.DidNotReceive().GetAsync<int>("k");
        await _protector.DidNotReceive().RunAsync(Arg.Any<string>(), Arg.Any<Func<Task<int?>>>());
    }

    [Test]
    public async Task UsesProtector_IfMissed()
    {
        ConfigureMissingLayerForKey(_mem, "k");
        ConfigureMissingLayerForKey(_redis, "k");
        ConfigureStampedeProtectorForKey("k");
        var expected = new DummyObj { Id = 7, Name = "zzz" };

        var result = await _engine.GetOrSetAsync("k", () => Task.FromResult<DummyObj?>(expected), TimeSpan.FromMinutes(1));

        Assert.That(result.Value, Is.EqualTo(expected));
        await _protector.Received(1).RunAsync("k", Arg.Any<Func<Task<CachulaCacheEntry<DummyObj>>>>());
    }

    [Test]
    public async Task WarmsAllLayersAboveFoundLevel_IfFoundInLowerLayer()
    {
        var expected = new DummyObj { Id = 99, Name = "from-redis" };
        ConfigureMissingLayerForKey(_mem, "k");
        ConfigureLayerResponse(_redis, "k", new CachulaCacheEntry<DummyObj>(expected, TimeSpan.Zero));
        ConfigureStampedeProtectorForKey("k");

        var result = await _engine.GetOrSetAsync(
            "k", TaskHelpers.NullRef<DummyObj>, TimeSpan.FromMinutes(1));

        Assert.That(result.Value, Is.EqualTo(expected));
        await _mem.Received(1).SetAsync(
            "k", Arg.Is<CachulaCacheEntry<DummyObj>>(x => x.Value == result.Value), Arg.Any<TimeSpan>());
    }

    [Test]
    public async Task CallsValueFactory_IfNotFoundAnywhere()
    {
        ConfigureMissingLayerForKey(_mem, "k");
        ConfigureMissingLayerForKey(_redis, "k");
        ConfigureStampedeProtectorForKey("k");
        var called = false;
        var expected = new DummyObj { Id = 42, Name = "test" };
        DummyObj Factory() { called = true; return expected; }

        var result = await _engine.GetOrSetAsync("k", () => Task.FromResult<DummyObj?>(Factory()), TimeSpan.FromMinutes(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(called, Is.True);
        });
    }

    [Test]
    public async Task WritesToAllLayers_IfLoadedFromFactory()
    {
        ConfigureMissingLayerForKey(_mem, "k");
        ConfigureMissingLayerForKey(_redis, "k");
        ConfigureStampedeProtectorForKey("k");
        var expected = new DummyObj { Id = 123, Name = "factory" };

        await _engine.GetOrSetAsync("k", () => Task.FromResult<DummyObj?>(expected), TimeSpan.FromMinutes(1));

        await _mem.Received(1).SetAsync("k", expected, Arg.Any<TimeSpan>());
        await _redis.Received(1).SetAsync("k", expected, Arg.Any<TimeSpan>());
    }

    [Test]
    public async Task ReturnsNull_IfFactoryReturnsNull()
    {
        ConfigureMissingLayerForKey(_mem, "k");
        ConfigureMissingLayerForKey(_redis, "k");
        ConfigureStampedeProtectorForKey("k");

        var result = await _engine.GetOrSetAsync("k", () => Task.FromResult<DummyObj?>(null), TimeSpan.FromMinutes(1));

        Assert.That(result.IsNull, Is.True);
    }

    [Test]
    public async Task WritesNullToAllLayers_IfFactoryReturnsNull()
    {
        ConfigureMissingLayerForKey(_mem, "k");
        ConfigureMissingLayerForKey(_redis, "k");
        ConfigureStampedeProtectorForKey("k");

        await _engine.GetOrSetAsync("k", TaskHelpers.NullRef<DummyObj>, TimeSpan.FromMinutes(1));

        await _mem.Received(1).SetAsync("k", Arg.Is<DummyObj>(x => x == null), Arg.Any<TimeSpan>());
        await _redis.Received(1).SetAsync("k", Arg.Is<DummyObj>(x => x == null), Arg.Any<TimeSpan>());
    }

    private void ConfigureMissingLayerForKey(ICacheLayer layer, string key)
    {
        layer.GetAsync<DummyObj>(key).Returns(CachulaCacheEntry<DummyObj>.Missed);
    }

    private void ConfigureLayerResponse(ICacheLayer layer, string key, CachulaCacheEntry<DummyObj> result)
    {
        layer.GetAsync<DummyObj>(key).Returns(result);
    }

    private void ConfigureStampedeProtectorForKey(string key)
    {
        _protector.RunAsync(key, Arg.Any<Func<Task<CachulaCacheEntry<DummyObj>>>>())
            .Returns(call =>
            {
                var loader = (Func<Task<CachulaCacheEntry<DummyObj>>>)call.Args()[1];
                return loader();
            });
    }
}
