namespace NServiceBus.Core.Tests.Features
{
    using System;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    public class FeatureServiceTests
    {
        [Test]
        public void Should_throw_for_feature_with_missing_service_dependency()
        {
            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            var featureWithMissingServiceDep = new FeatureDependingOnService();

            featureSettings.Add(featureWithMissingServiceDep);

            Assert.Throws<Exception>(() => featureSettings.SetupFeatures(null, null));
        }

        [Test]
        public void Should_activate_feature_with_satisfied_service_dependency()
        {
            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            var dependingFeature = new FeatureDependingOnService();

            var providingFeature = new FeatureProvidingService();

            featureSettings.Add(dependingFeature);
            featureSettings.Add(providingFeature);

            featureSettings.SetupFeatures(null, null);

            Assert.True(dependingFeature.IsActive, "featureWithMissingServiceDep should be activated");
        }

        class FeatureDependingOnService : TestFeature, IRequireService<SomeService>
        {
            public FeatureDependingOnService()
            {
                EnableByDefault();
            }
        }

        class FeatureProvidingService : TestFeature, IProvideService<SomeService>
        {
            public FeatureProvidingService()
            {
                EnableByDefault();
            }
        }
        class SomeService : IFeatureService
        {

        }
    }
}