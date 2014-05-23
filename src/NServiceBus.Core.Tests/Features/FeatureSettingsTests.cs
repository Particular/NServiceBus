namespace NServiceBus.Core.Tests.Features
{
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class FeatureSettingsTests
    {
        [Test]
        public void Should_allow_for_features_to_be_disabled()
        {
            var featureSettings = new FeatureSettings(new SettingsHolder())
            {
               
            };

            Assert.False(featureSettings.IsActivated<MyFeature>());

            featureSettings.EnableByDefault<MyFeature>();

            Assert.True(featureSettings.IsActivated<MyFeature>());

            featureSettings.Disable<MyFeature>();

            Assert.False(featureSettings.IsActivated<MyFeature>());
        }


        [Test]
        public void Should_check_activation_conditions()
        {
            var featureWithTrueCondition = new MyFeatureWithTrueActivationCondition();
            var featureWithFalseCondition = new MyFeatureWithFalseActivationCondition();

            var featureSettings = new FeatureSettings(new SettingsHolder())
            {
               featureWithTrueCondition,
               featureWithFalseCondition
            };

            featureSettings.SetupFeatures();

            Assert.True(featureWithTrueCondition.WasActivated);
            Assert.False(featureWithFalseCondition.WasActivated);
        }


        public class MyFeature : TestFeature
        {
        }

        public class MyFeatureWithTrueActivationCondition : TestFeature
        {
            public MyFeatureWithTrueActivationCondition()
            {
                EnableByDefault();
                Prerequisite(c => true);
            }
        }

        public class MyFeatureWithFalseActivationCondition : TestFeature
        {
            public MyFeatureWithFalseActivationCondition()
            {
                EnableByDefault();
                Prerequisite(c => false);
            }
        }

    }

    public class TestFeature : Feature
    {
        public bool WasActivated { get; set; }

        protected override void Setup(FeatureConfigurationContext context)
        {
            WasActivated = true;
        }
    }
}