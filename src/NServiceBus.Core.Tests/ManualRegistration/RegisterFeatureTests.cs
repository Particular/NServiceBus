namespace NServiceBus.Core.Tests.ManualRegistration;

using System;
using NServiceBus.Features;
using NUnit.Framework;

[TestFixture]
public class RegisterFeatureTests
{
    [Test]
    public void RegisterFeature_Generic_Should_Store_Feature_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterFeature<TestFeature>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredFeatures features), Is.True);
        Assert.That(features.FeatureTypes, Has.Count.EqualTo(1));
        Assert.That(features.FeatureTypes, Does.Contain(typeof(TestFeature)));
    }

    [Test]
    public void RegisterFeature_NonGeneric_Should_Store_Feature_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterFeature(typeof(TestFeature));

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredFeatures features), Is.True);
        Assert.That(features.FeatureTypes, Has.Count.EqualTo(1));
        Assert.That(features.FeatureTypes, Does.Contain(typeof(TestFeature)));
    }

    [Test]
    public void RegisterFeature_Multiple_Should_Store_All()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterFeature<TestFeature>();
        config.RegisterFeature<AnotherTestFeature>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredFeatures features), Is.True);
        Assert.That(features.FeatureTypes, Has.Count.EqualTo(2));
        Assert.That(features.FeatureTypes, Does.Contain(typeof(TestFeature)));
        Assert.That(features.FeatureTypes, Does.Contain(typeof(AnotherTestFeature)));
    }

    [Test]
    public void RegisterFeature_Null_FeatureType_Should_Throw()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        Assert.Throws<ArgumentNullException>(() => config.RegisterFeature(null));
    }

    public class TestFeature : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }

    public class AnotherTestFeature : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}

