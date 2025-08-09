using Cachula.Core;
using Cachula.Interfaces;

namespace Cachula.Tests.Core.CachulaEngineTests;

[TestFixture]
public class CachulaEngine_SetAsync_Tests
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
    public async Task SetAsync_CallsSetAsyncOnAllLayers_WithCorrectArguments()
    {
        var value = new DummyObj { Id = 1, Name = "set" };
        var ttl = TimeSpan.FromMinutes(5);

        await _engine.SetAsync("k", value, ttl);

        await _mem.Received(1).SetAsync("k", value, ttl);
        await _redis.Received(1).SetAsync("k", value, ttl);
    }

    [Test]
    public void SetAsync_ThrowsArgumentNullException_WhenKeyIsNull()
    {
        var value = new DummyObj { Id = 2, Name = "null-key" };
        var ttl = TimeSpan.FromMinutes(1);

        Assert.ThrowsAsync<ArgumentNullException>(() => _engine.SetAsync(null!, value, ttl));
    }

    [Test]
    public void SetAsync_DoesNotThrowArgumentNullException_WhenValueIsNullForReferenceType()
    {
        var ttl = TimeSpan.FromMinutes(1);

        Assert.DoesNotThrowAsync(() => _engine.SetAsync<DummyObj>("k", null!, ttl));
    }
}
