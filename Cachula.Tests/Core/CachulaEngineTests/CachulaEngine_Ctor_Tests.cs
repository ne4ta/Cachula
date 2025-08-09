using Cachula.Core;
using Cachula.Interfaces;

namespace Cachula.Tests.Core.CachulaEngineTests;

[TestFixture]
public class CachulaEngine_Ctor_Tests
{
    private IStampedeProtector _protector;

    [SetUp]
    public void SetUp()
    {
        _protector = Substitute.For<IStampedeProtector>();
    }

    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenLayersIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CachulaEngine(null!, _protector));
    }

    [Test]
    public void Constructor_ThrowsArgumentException_WhenLayersIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new CachulaEngine(new List<ICacheLayer>(), _protector));
    }

    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenProtectorIsNull()
    {
        var layer = Substitute.For<ICacheLayer>();

        Assert.Throws<ArgumentNullException>(() => new CachulaEngine([layer], null!));
    }

    [Test]
    public void Constructor_Succeeds_WithValidArguments()
    {
        var layer = Substitute.For<ICacheLayer>();
        var protector = Substitute.For<IStampedeProtector>();

        var engine = new CachulaEngine([layer], protector);

        Assert.That(engine, Is.Not.Null);
    }
}
