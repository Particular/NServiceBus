namespace NServiceBus.Core.Tests.Features;

using NUnit.Framework;

[TestFixture]
public partial class FeatureTests
{
    [Test]
    public void Should_be_disabled_be_default()
    {
        var feature = new MyFeature();

        Assert.That(feature.IsEnabledByDefault, Is.False);
    }

    public class MyFeature : TestFeature;
}