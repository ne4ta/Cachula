using Cachula.Core;

namespace Cachula.Tests.Core.InMemorySingleFlightProtectorTests;

public class InMemorySingleFlightProtector_RunManyAsync_Tests
{
    [Test]
    public async Task RunManyAsync_ReturnsLoaderResults()
    {
        var protector = new InMemorySingleFlightProtector();
        var keys = new[] { "a", "b", "c" };
        var values = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };

        var resultTasks = protector.RunManyAsync<int>(keys, async ks =>
        {
            await Task.Delay(10);
            return values.Where(kv => ks.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        });

        var results = await Task.WhenAll(resultTasks.Values);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, results);
    }

    [Test]
    public async Task RunManyAsync_OnlyOneLoaderRunsForSameKey()
    {
        var protector = new InMemorySingleFlightProtector();
        var keys = new[] { "x", "y" };
        int loaderCallCount = 0;
        async Task<IDictionary<string, int>> Loader(IEnumerable<string> ks)
        {
            loaderCallCount++;
            await Task.Delay(20);
            return ks.ToDictionary(k => k, k => k.Length);
        }

        var tasks1 = protector.RunManyAsync(keys, Loader);
        var tasks2 = protector.RunManyAsync(keys, Loader);
        var results1 = await Task.WhenAll(tasks1.Values);
        var results2 = await Task.WhenAll(tasks2.Values);

        Assert.That(loaderCallCount, Is.EqualTo(1));
        CollectionAssert.AreEqual(results1, results2);
    }

    [Test]
    public async Task RunManyAsync_DifferentKeys_SeparateLoaders()
    {
        var protector = new InMemorySingleFlightProtector();
        var keys1 = new[] { "a", "b" };
        var keys2 = new[] { "c", "d" };
        int loaderCallCount = 0;
        async Task<IDictionary<string, int>> Loader(IEnumerable<string> ks)
        {
            loaderCallCount++;
            await Task.Delay(10);
            return ks.ToDictionary(k => k, k => k.Length);
        }

        var tasks1 = protector.RunManyAsync(keys1, Loader);
        var tasks2 = protector.RunManyAsync(keys2, Loader);
        await Task.WhenAll(tasks1.Values.Concat(tasks2.Values));

        Assert.That(loaderCallCount, Is.EqualTo(2));
    }

    [Test]
    public void RunManyAsync_LoaderThrows_PropagatesException()
    {
        var protector = new InMemorySingleFlightProtector();
        var keys = new[] { "fail1", "fail2" };
        Task<IDictionary<string, int>> Loader(IEnumerable<string> ks) => throw new InvalidOperationException("fail");

        var tasks = protector.RunManyAsync(keys, Loader);
        foreach (var task in tasks.Values)
        {
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
            Assert.That(ex.Message, Is.EqualTo("fail"));
        }
    }
}
