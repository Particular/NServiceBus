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
        public void Should_not_activate_features_with_unmet_dependencies_all()
        {
            var dependingFeature = new DependingFeature1();
            var feature = new MyFeature();

            var featureSettings = new FeatureActivator(new SettingsHolder());

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            featureSettings.SetupFeatures(null);

            Assert.False(dependingFeature.IsActive);
        }

        [Test]
        public void Should_not_activate_features_with_unmet_dependencies_any()
        {
            var dependingFeature = new DependingFeature2();
            var feature = new MyFeature();

            var featureSettings = new FeatureActivator(new SettingsHolder());

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            featureSettings.SetupFeatures(null);

            Assert.False(dependingFeature.IsActive);
        }

        [Test]
        public void Should_activate_upstream_deps_first()
        {
            var order = new List<Feature>();

            var dependingFeature = new DependingFeature1
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

            featureSettings.SetupFeatures(null);

            Assert.True(dependingFeature.IsActive);

            Assert.IsInstanceOf<MyFeature>(order.First(), "Upstream deps should be activated first");
        }


        public class MyFeature : TestFeature
        {
      
        }

        public class MyFeature2 : TestFeature
        {

        }

        public class MyFeature3 : TestFeature
        {

        }

        public class DependingFeature1 : TestFeature
        {
            public DependingFeature1()
            {
                EnableByDefault();
                DependsOn<MyFeature>();
            }
        }

        public class DependingFeature2 : TestFeature
        {
            public DependingFeature2()
            {
                EnableByDefault();
                DependsOnAny(typeof(MyFeature), typeof(MyFeature2), typeof(MyFeature3));
            }
        }
    }
}