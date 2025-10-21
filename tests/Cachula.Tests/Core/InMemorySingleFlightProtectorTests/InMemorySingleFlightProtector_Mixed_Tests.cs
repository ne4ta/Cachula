using System.Collections.Concurrent;
using Cachula.Core;

namespace Cachula.Tests.Core.InMemorySingleFlightProtectorTests;

[TestFixture]
public class InMemorySingleFlightProtector_Mixed_Tests
{
    [Test]
    public async Task RunAsync_And_RunManyAsync_SameKey_OnlyOneLoaderRuns()
    {
        var protector = new InMemorySingleFlightProtector();
        int loaderCallCount = 0;
        async Task<int> LoaderSingle() {
            loaderCallCount++;
            await Task.Delay(30);
            return 42;
        }
        async Task<IDictionary<string, int>> LoaderBatch(IEnumerable<string> keys)
        {
            loaderCallCount++;
            await Task.Delay(30);
            return keys.ToDictionary(k => k, _ => 42);
        }

        var key = "shared";
        var singleTask = protector.RunAsync(key, LoaderSingle);
        var batchTasks = protector.RunManyAsync([key], LoaderBatch);
        var batchTask = batchTasks[key];

        var results = await Task.WhenAll(singleTask, batchTask);
        Assert.Multiple(() =>
        {
            Assert.That(results[0], Is.EqualTo(42));
            Assert.That(results[1], Is.EqualTo(42));
            Assert.That(loaderCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task RunAsync_And_RunManyAsync_IntersectingAndNonIntersectingKeys_OnlyOneLoaderPerKey()
    {
        var protector = new InMemorySingleFlightProtector();
        var loaderCallCount = new ConcurrentDictionary<string, int>();

        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<int> LoaderSingle(string key)
        {
            await startGate.Task;
            loaderCallCount.AddOrUpdate(key, 1, (_, v) => v + 1);
            await Task.Delay(50);
            return key.Length;
        }

        async Task<IDictionary<string, int>> LoaderBatch(IEnumerable<string> keys)
        {
            await startGate.Task;
            foreach (var key in keys)
            {
                loaderCallCount.AddOrUpdate(key, 1, (_, v) => v + 1);
            }
            await Task.Delay(60);
            return keys.ToDictionary(k => k, k => k.Length);
        }

        var t1 = protector.RunAsync("a", () => LoaderSingle("a"));
        var t2 = protector.RunAsync("b", () => LoaderSingle("b"));
        var t3 = protector.RunManyAsync(["a", "b", "c"], LoaderBatch);
        var t4 = protector.RunManyAsync(["b", "c", "d"], LoaderBatch);
        var t5 = protector.RunAsync("e", () => LoaderSingle("e"));
        var t6 = protector.RunManyAsync(["d", "e"], LoaderBatch);
        var t7 = protector.RunAsync("d", () => LoaderSingle("d"));

        startGate.TrySetResult();

        var allTasks = new List<Task<int>> { t1, t2, t5, t7 };
        allTasks.AddRange(t3.Values);
        allTasks.AddRange(t4.Values);
        allTasks.AddRange(t6.Values);

        var results = await Task.WhenAll(allTasks);
        foreach (var key in new[] { "a", "b", "c", "d", "e" })
        {
            Assert.That(results.Count(r => r == key.Length), Is.GreaterThan(0), $"Key {key} must be present");
        }

        foreach (var key in new[] { "a", "b", "c", "d", "e" })
        {
            Assert.That(loaderCallCount[key], Is.EqualTo(1), $"Loader for key {key} called {loaderCallCount[key]} times");
        }
    }

    [Test]
    public async Task ManyParallelRunAsync_And_RunManyAsync_MixedOverlap_OnlyOneLoaderPerKey_WithExpectedValue()
    {
        var protector = new InMemorySingleFlightProtector();
        var loaderCallCount = new ConcurrentDictionary<string, int>();

        async Task<string> LoaderSingle(string key)
        {
            loaderCallCount.AddOrUpdate(key, 1, (_, v) => v + 1);
            await Task.Delay(10);
            return key;
        }

        async Task<IDictionary<string, string>> LoaderBatch(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                loaderCallCount.AddOrUpdate(key, 1, (_, v) => v + 1);
            }

            await Task.Delay(15);
            return keys.ToDictionary(k => k, k => k);
        }

        var allKeys = new[] { "1", "2", "3", "4", "5" };
        var tasks = new List<Task<string>>();

        tasks.AddRange(allKeys.Select(k => protector.RunAsync(k, () => LoaderSingle(k))));

        var batch1 = protector.RunManyAsync(["1", "2"], LoaderBatch);
        var batch2 = protector.RunManyAsync(["2", "3"], LoaderBatch);
        var batch3 = protector.RunManyAsync(["4", "5", "1"], LoaderBatch);
        tasks.AddRange(batch1.Values);
        tasks.AddRange(batch2.Values);
        tasks.AddRange(batch3.Values);
        var results = await Task.WhenAll(tasks);

        foreach (var key in allKeys)
        {
            Assert.That(results.Count(r => r == key), Is.GreaterThan(0), $"Key {key} must be present");
        }

        foreach (var key in allKeys)
        {
            Assert.That(loaderCallCount[key], Is.EqualTo(1), $"Loader for key {key} called {loaderCallCount[key]} times");
        }
    }

    [Test]
    public async Task RunManyAsync_MultipleBatchesWithPartialOverlap_OnlyOneLoaderPerKey()
    {
        var protector = new InMemorySingleFlightProtector();
        var loaderCallCount = new ConcurrentDictionary<string, int>();
        async Task<IDictionary<string, int>> LoaderBatch(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                loaderCallCount.AddOrUpdate(key, 1, (_, v) => v + 1);
            }

            await Task.Delay(10);
            return keys.ToDictionary(k => k, k => k.Length);
        }

        var batch1 = protector.RunManyAsync(["a", "b", "c"], LoaderBatch);
        var batch2 = protector.RunManyAsync(["b", "c", "d"], LoaderBatch);
        var batch3 = protector.RunManyAsync(["c", "d", "e"], LoaderBatch);
        var batch4 = protector.RunManyAsync(["e", "f"], LoaderBatch);
        var allTasks = batch1.Values.Concat(batch2.Values).Concat(batch3.Values).Concat(batch4.Values);
        var results = await Task.WhenAll(allTasks);
        foreach (var key in new[] { "a", "b", "c", "d", "e", "f" })
        {
            Assert.That(results.Count(r => r == key.Length), Is.GreaterThan(0), $"Key {key} must be present");
        }

        foreach (var key in new[] { "a", "b", "c", "d", "e", "f" })
        {
            Assert.That(loaderCallCount[key], Is.EqualTo(1), $"Loader for key {key} called {loaderCallCount[key]} times");
        }
    }
}
