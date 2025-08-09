using System.Text.Json;
using Cachula.Layers;

namespace Cachula.Tests.Layers;

[TestFixture]
public class CachulaCacheEntryTests
{
    [Test]
    public void Null_ReturnsNullEntry()
    {
        var entry = CachulaCacheEntry<string>.Null;

        Assert.Multiple(() =>
        {
            Assert.That(entry.IsNull, Is.True);
            Assert.That(entry.Value, Is.Null);
            Assert.That(entry.IsMissed, Is.False);
        });
    }

    [Test]
    public void Missed_ReturnsMissedEntry()
    {
        var entry = CachulaCacheEntry<int>.Missed;

        Assert.Multiple(() =>
        {
            Assert.That(entry.IsMissed, Is.True);
            Assert.That(entry.IsNull, Is.False);
        });
    }

    [Test]
    public void Ctor_SetsProperties_WithNotNullValue()
    {
        var now = DateTimeOffset.UtcNow;
        var ttl = TimeSpan.FromMinutes(5);

        var entry = new CachulaCacheEntry<string>("abc", ttl);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Value, Is.EqualTo("abc"));
            Assert.That(entry.IsNull, Is.False);
            Assert.That(entry.Ttl, Is.EqualTo(ttl));
            Assert.That(entry.CreatedAt, Is.GreaterThanOrEqualTo(now).And.LessThanOrEqualTo(DateTimeOffset.UtcNow));
        });
    }

    [Test]
    public void Constructor_SetsIsNull_WithNullValue()
    {
        var entry = new CachulaCacheEntry<string>(null, TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(entry.IsNull, Is.True);
            Assert.That(entry.Value, Is.Null);
        });
    }

    [Test]
    public void Expiration_ComputedCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var ttl = TimeSpan.FromMinutes(2);

        var entry = new CachulaCacheEntry<int>(42, ttl) { CreatedAt = now };

        Assert.That(entry.Expiration, Is.EqualTo(now.Add(ttl)));
    }

    [Test]
    public void IsExpired_ReturnsTrue_WhenExpired()
    {
        var entry = new CachulaCacheEntry<int>(1, TimeSpan.FromSeconds(-1))
            { CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10) };

        Assert.That(entry.IsExpired, Is.True);
    }

    [Test]
    public void IsExpired_ReturnsFalse_WhenNotExpired()
    {
        var entry = new CachulaCacheEntry<int>(1, TimeSpan.FromMinutes(10)) { CreatedAt = DateTimeOffset.UtcNow };

        Assert.That(entry.IsExpired, Is.False);
    }

    [Test]
    public void CanBeSerializedAndDeserialized()
    {
        var entry = new CachulaCacheEntry<string>("test", TimeSpan.FromSeconds(30));

        var json = JsonSerializer.Serialize(entry);
        var deserialized = JsonSerializer.Deserialize<CachulaCacheEntry<string>>(json);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized!.Value, Is.EqualTo("test"));
            Assert.That(deserialized.Ttl, Is.EqualTo(entry.Ttl));
            Assert.That(deserialized.CreatedAt, Is.EqualTo(entry.CreatedAt));
            Assert.That(deserialized.IsNull, Is.False);
            Assert.That(deserialized.IsMissed, Is.False);
            Assert.That(deserialized.Expiration, Is.EqualTo(entry.Expiration));
        });
    }
}
