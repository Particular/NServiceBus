namespace NServiceBus.Core.Tests.Features;

using NUnit.Framework;

[TestFixture]
public class FeatureTests
{
    [Test]
    public void Should_be_disabled_be_default()
    {
        var feature = new MyFeature();

        Assert.That(feature.IsEnabledByDefault, Is.False);
    }

    [Test]
    public void Should_be_allow_features_to_request_being_enabled_by_default()
    {
        var feature = new MyEnabledByDefaultFeature();

        Assert.That(feature.IsEnabledByDefault, Is.True);
    }


    public class MyFeature : TestFeature;


    public class MyEnabledByDefaultFeature : TestFeature
    {
        public MyEnabledByDefaultFeature() => EnableByDefault();
    }
}