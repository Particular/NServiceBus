namespace NServiceBus.Core.Tests.Features
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class FeatureDependencyTests
    {
        [Test]
        public void Should_not_activate_features_with_unmet_dependencies()
        {
            var dependingFeature = new DependingFeature();
            var feature = new MyFeature();

            var featureSettings = new FeatureActivator(new SettingsHolder());

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            featureSettings.SetupFeatures();

            Assert.False(dependingFeature.IsActive);
        }


        [Test]
        public void Should_activate_upstream_deps_first()
        {
            var order = new List<Feature>();

            var dependingFeature = new DependingFeature
            {
                OnActivation = f => order.Add(f)
            };
            var feature = new MyFeature
            {
                OnActivation = f => order.Add(f)
            };

            var settings = new SettingsHolder();


            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            settings.EnableFeatureByDefault<MyFeature>();

            featureSettings.SetupFeatures();

            Assert.True(dependingFeature.IsActive);

            Assert.IsInstanceOf<MyFeature>(order.First(), "Upstream deps should be activated first");
        }


        public class MyFeature : TestFeature
        {
      
        }

        public class DependingFeature : TestFeature
        {
            public DependingFeature()
            {
                EnableByDefault();
                DependsOn<MyFeature>();
            }
        }

        
    }
}