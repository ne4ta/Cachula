using Cachula.Core;

namespace Cachula.Tests.Core.InMemorySingleFlightProtectorTests;

[TestFixture]
public class InMemorySingleFlightProtector_RunAsync_Tests
{
    [Test]
    public async Task ReturnsLoaderResult()
    {
        var protector = new InMemorySingleFlightProtector();
        var result = await protector.RunAsync("key1", () => Task.FromResult(42));
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task OnlyOneLoaderRunsForSameKey()
    {
        var protector = new InMemorySingleFlightProtector();
        int loaderCallCount = 0;
        async Task<int> Loader()
        {
            loaderCallCount++;
            await Task.Delay(50);
            return 123;
        }

        var task1 = protector.RunAsync("key2", Loader);
        var task2 = protector.RunAsync("key2", Loader);
        var results = await Task.WhenAll(task1, task2);

        Assert.Multiple(() =>
        {
            Assert.That(loaderCallCount, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(123));
            Assert.That(results[1], Is.EqualTo(123));
        });
    }

    [Test]
    public async Task DifferentKeys_RunSeparately()
    {
        const string key1 = "keyA";
        const string key2 = "keyB";
        var protector = new InMemorySingleFlightProtector();
        var loaderCallCount = 0;

        async Task<int> Loader(string key)
        {
            loaderCallCount++;
            await Task.Delay(10);
            return key == key1 ? 1 : 2;
        }

        var task1 = protector.RunAsync(key1, () => Loader(key1));
        var task2 = protector.RunAsync(key2, () => Loader(key2));
        var results = await Task.WhenAll(task1, task2);

        Assert.Multiple(() =>
        {
            Assert.That(loaderCallCount, Is.EqualTo(2));
            Assert.That(results[1], Is.Not.EqualTo(results[0]));
        });
    }

    [Test]
    public void LoaderThrows_PropagatesException()
    {
        var protector = new InMemorySingleFlightProtector();
        Task<int> Loader() => throw new InvalidOperationException("fail");

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await protector.RunAsync("keyX", Loader));

        Assert.That(ex.Message, Is.EqualTo("fail"));
    }

    [Test]
    public async Task LoaderReturnsNull()
    {
        var protector = new InMemorySingleFlightProtector();
        var key = "a";

        async Task<int?> Loader()
        {
            await Task.Delay(10);
            return null;
        }

        var result = await protector.RunAsync(key, Loader);

        Assert.That(result, Is.Null);
    }
}
