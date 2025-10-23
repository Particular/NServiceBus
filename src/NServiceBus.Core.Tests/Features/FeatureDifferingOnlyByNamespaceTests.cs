namespace NServiceBus.Core.Tests.Features
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class FeatureDifferingOnlyByNamespaceTests
    {
        [Test]
        public void Should_activate_upstream_dependencies_first()
        {
            var order = new List<Feature>();

            var dependingFeature = new NamespaceB.MyFeature
            {
                OnActivation = f => order.Add(f)
            };
            var feature = new NamespaceA.MyFeature
            {
                OnActivation = f => order.Add(f)
            };

            var settings = new SettingsHolder();
            var featureFactory = new FakeFeatureFactory();
            var featureSettings = new FeatureComponent.Settings(featureFactory);
            settings.Set(featureSettings);
            var featureComponent = new FeatureComponent(featureSettings);

            featureFactory.Add(dependingFeature, feature);

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            settings.EnableFeature<NamespaceA.MyFeature>();

            featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dependingFeature.IsActive, Is.True);

                Assert.That(order.First(), Is.InstanceOf<NamespaceA.MyFeature>(), "Upstream dependencies should be activated first");
            }
        }
    }
}

namespace NamespaceA
{
    using NServiceBus.Core.Tests.Features;

    public class MyFeature : TestFeature
    {
    }
}

namespace NamespaceB
{
    using NServiceBus.Core.Tests.Features;

    public class MyFeature : TestFeature
    {
        public MyFeature()
        {
            EnableByDefault();
            DependsOn<NamespaceA.MyFeature>();
        }
    }
}