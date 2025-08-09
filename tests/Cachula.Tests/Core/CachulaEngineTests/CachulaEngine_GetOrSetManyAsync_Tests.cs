using Cachula.Core;
using Cachula.Interfaces;
using Cachula.Layers;

namespace Cachula.Tests.Core.CachulaEngineTests;

[TestFixture]
public class CachulaEngine_GetOrSetManyAsync_Tests
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
        var keys = new[] { "a", "b" };
        var expected = new Dictionary<string, CachulaCacheEntry<DummyObj>>
        {
            ["a"] = new(new DummyObj { Id = 1, Name = "A" }, TimeSpan.Zero),
            ["b"] = new(new DummyObj { Id = 2, Name = "B" }, TimeSpan.Zero),
        };
        ConfigureLayerResponse(_mem, keys, expected);

        var result = await _engine.GetOrSetManyAsync(keys,
            _ => Task.FromResult<IDictionary<string, DummyObj>>(new Dictionary<string, DummyObj>()),
            TimeSpan.FromMinutes(1));

        Assert.That(result, Is.EquivalentTo(expected));
        await _redis.DidNotReceive().GetManyAsync<DummyObj>(Arg.Any<IEnumerable<string>>());
        _protector.DidNotReceive().RunManyAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Func<IEnumerable<string>, Task<IDictionary<string, CachulaCacheEntry<DummyObj>>>>>());
    }

    [Test]
    public async Task UsesProtector_IfFirstLayerMiss()
    {
        var keys = new[] { "a", "b" };
        ConfigureMissingLayerForKeys(_mem, keys);
        ConfigureStampedeProtectorForKeys(keys);

        _redis.GetManyAsync<DummyObj>(Arg.Any<IEnumerable<string>>())
            .Returns(Task.FromResult<IDictionary<string, CachulaCacheEntry<DummyObj>>>(
                new Dictionary<string, CachulaCacheEntry<DummyObj>>()));

        _redis.GetAsync<DummyObj>(keys[0], CancellationToken.None).Returns(call =>
        {
            var key = call.Arg<string>();
            var cacheEntry = new CachulaCacheEntry<DummyObj>(new DummyObj { Id = key[0], Name = key }, TimeSpan.Zero);
            return Task.FromResult(cacheEntry);
        });

        var result = await _engine.GetOrSetManyAsync(keys,
            ks => Task.FromResult<IDictionary<string, DummyObj>>(ks.ToDictionary(k => k,
                k => new DummyObj { Id = k[0], Name = k })), TimeSpan.FromMinutes(1));

        foreach (var key in keys)
        {
            Assert.That(result[key].Value, Is.EqualTo(new DummyObj { Id = key[0], Name = key }));
        }

        _protector.Received(1).RunManyAsync(
            Arg.Is<IEnumerable<string>>(k => k.SequenceEqual(keys)),
            Arg.Any<Func<IEnumerable<string>, Task<IDictionary<string, CachulaCacheEntry<DummyObj>>>>>());
    }

    [Test]
    public async Task WarmsAllLayersAboveFoundLevel_IfFoundInLowerLayer()
    {
        var keys = new[] { "a", "b" };
        var ttl = TimeSpan.FromMinutes(1);
        ConfigureMissingLayerForKeys(_mem, keys);

        var redisResult = new Dictionary<string, CachulaCacheEntry<DummyObj>>
        {
            ["a"] = new(new DummyObj { Id = 1, Name = "A" }, TimeSpan.Zero)
        };
        ConfigureLayerResponse(_redis, keys, redisResult);

        ConfigureStampedeProtectorForKeys(keys);

        await _engine.GetOrSetManyAsync(keys,
            _ => Task.FromResult<IDictionary<string, DummyObj>>(new Dictionary<string, DummyObj>()),
            ttl);

        // Should be warmed only the mem layer for key "a"
        await _mem.Received(1).SetManyAsync(
            Arg.Is<IDictionary<string, DummyObj>>(d => d.Count == 1 && d.ContainsKey("a") && d["a"].Id == 1),
            ttl,
            Arg.Any<CancellationToken>());
        await _redis.DidNotReceive().SetManyAsync(Arg.Any<IDictionary<string, DummyObj>>(), Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CallsValueFactory_IfNotFoundAnywhere()
    {
        var keys = new[] { "a", "b" };
        var ttl = TimeSpan.FromMinutes(1);
        ConfigureMissingLayerForKeys(_mem, keys);
        ConfigureMissingLayerForKeys(_redis, keys);
        ConfigureStampedeProtectorForKeys(keys);

        var expected = new Dictionary<string, DummyObj>
        {
            ["a"] = new() { Id = 1, Name = "A" },
            ["b"] = new() { Id = 2, Name = "B" }
        };

        var result = await _engine.GetOrSetManyAsync(keys, _ => Task.FromResult<IDictionary<string, DummyObj>>(expected), ttl);

        Assert.Multiple(() =>
        {
            Assert.That(result["a"].Value, Is.EqualTo(expected["a"]));
            Assert.That(result["b"].Value, Is.EqualTo(expected["b"]));
        });
    }

    [Test]
    public async Task WritesToAllLayers_IfLoadedFromFactory()
    {
        var keys = new[] { "a", "b" };
        var ttl = TimeSpan.FromMinutes(1);
        ConfigureMissingLayerForKeys(_mem, keys);
        ConfigureMissingLayerForKeys(_redis, keys);
        ConfigureStampedeProtectorForKeys(keys);

        var expected = new Dictionary<string, DummyObj>
        {
            ["a"] = new() { Id = 1, Name = "A" },
            ["b"] = new() { Id = 2, Name = "B" }
        };

        await _engine.GetOrSetManyAsync(keys, _ => Task.FromResult<IDictionary<string, DummyObj>>(expected), ttl);

        // Should write to all layers with the values from the factory
        await _mem.Received(1).SetManyAsync(
            Arg.Is<IDictionary<string, DummyObj>>(d => d.Count == 2 && d["a"].Id == 1 && d["b"].Id == 2),
            ttl,
            Arg.Any<CancellationToken>());
        await _redis.Received(1).SetManyAsync(
            Arg.Is<IDictionary<string, DummyObj>>(d => d.Count == 2 && d["a"].Id == 1 && d["b"].Id == 2),
            ttl,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReturnsNullEntry_IfFactoryReturnsNull()
    {
        var keys = new[] { "a", "b" };
        var ttl = TimeSpan.FromMinutes(1);
        ConfigureMissingLayerForKeys(_mem, keys);
        ConfigureMissingLayerForKeys(_redis, keys);
        ConfigureStampedeProtectorForKeys(keys);

        var factoryResult = new Dictionary<string, DummyObj?>
        {
            ["a"] = null,
            ["b"] = null
        };

        var result = await _engine.GetOrSetManyAsync(keys, _ => Task.FromResult<IDictionary<string, DummyObj?>>(factoryResult), ttl);

        Assert.Multiple(() =>
        {
            Assert.That(result["a"].IsNull, Is.True);
            Assert.That(result["b"].IsNull, Is.True);
        });
    }

    [Test]
    public async Task ReturnsNullEntry_IfNullInMemoryCache()
    {
        var keys = new[] { "a", "b" };
        var expected = new Dictionary<string, CachulaCacheEntry<DummyObj>>
        {
            ["a"] = CachulaCacheEntry<DummyObj>.Null,
            ["b"] = CachulaCacheEntry<DummyObj>.Null
        };
        ConfigureLayerResponse(_mem, keys, expected);

        var result = await _engine.GetOrSetManyAsync(keys,
            _ => Task.FromResult<IDictionary<string, DummyObj>>(new Dictionary<string, DummyObj>
            {
                {"a", new DummyObj { Id = 1, Name = "A" }},
                {"b", new DummyObj { Id = 2, Name = "B" }}
            }),
            TimeSpan.FromMinutes(1));

        Assert.Multiple(() =>
        {
            Assert.That(result["a"].IsNull, Is.True);
            Assert.That(result["b"].IsNull, Is.True);
        });
    }

    [Test]
    public async Task ReturnsNullEntry_IfNullInRedisCache()
    {
        var keys = new[] { "a", "b" };
        ConfigureMissingLayerForKeys(_mem, keys);
        var expected = new Dictionary<string, CachulaCacheEntry<DummyObj>>
        {
            ["a"] = CachulaCacheEntry<DummyObj>.Null,
            ["b"] = CachulaCacheEntry<DummyObj>.Null
        };
        ConfigureLayerResponse(_redis, keys, expected);
        ConfigureStampedeProtectorForKeys(keys);

        var result = await _engine.GetOrSetManyAsync(keys,
            _ => Task.FromResult<IDictionary<string, DummyObj>>(new Dictionary<string, DummyObj>
            {
                {"a", new DummyObj { Id = 1, Name = "A" }},
                {"b", new DummyObj { Id = 2, Name = "B" }}
            }),
            TimeSpan.FromMinutes(1));

        Assert.Multiple(() =>
        {
            Assert.That(result["a"].IsNull, Is.True);
            Assert.That(result["b"].IsNull, Is.True);
        });
    }

    private void ConfigureMissingLayerForKeys(ICacheLayer layer, string[] keys)
    {
        layer.GetManyAsync<DummyObj>(Arg.Is<IEnumerable<string>>(k => k.SequenceEqual(keys)))
            .Returns(new Dictionary<string, CachulaCacheEntry<DummyObj>>());
    }

    private void ConfigureLayerResponse(
        ICacheLayer layer, IEnumerable<string> requestedKeys, Dictionary<string, CachulaCacheEntry<DummyObj>> result)
    {
        layer.GetManyAsync<DummyObj>(Arg.Is<IEnumerable<string>>(k => k.SequenceEqual(requestedKeys)))
            .Returns(result);
    }

    private void ConfigureStampedeProtectorForKeys(string[] keys)
    {
        _protector.RunManyAsync(
                Arg.Is<IEnumerable<string>>(k => k.SequenceEqual(keys)),
                Arg.Any<Func<IEnumerable<string>, Task<IDictionary<string, CachulaCacheEntry<DummyObj>>>>>())
            .Returns(call =>
            {
                var inputKeys = ((IEnumerable<string>)call.Args()[0]).ToArray();
                var loader = (Func<IEnumerable<string>, Task<IDictionary<string, CachulaCacheEntry<DummyObj>>>>)call.Args()[1];

                var loaderTask = loader(inputKeys);
                var dict = new Dictionary<string, Task<CachulaCacheEntry<DummyObj>>>();
                foreach (var key in inputKeys)
                {
                    dict[key] = loaderTask.ContinueWith(t => t.Result[key]);
                }

                return dict;
            });
    }
}
