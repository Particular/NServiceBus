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
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            settings.EnableFeatureByDefault<NamespaceA.MyFeature>();

            featureSettings.SetupFeatures(null, null, null);

            Assert.True(dependingFeature.IsActive);

            Assert.IsInstanceOf<NamespaceA.MyFeature>(order.First(), "Upstream dependencies should be activated first");
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