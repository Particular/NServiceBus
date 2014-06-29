namespace NServiceBus.Core.Tests.Features
{
    using System;
    using System.Linq;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class FeatureSettingsTests
    {

        [Test]
        public void Should_check_activation_conditions()
        {
            var featureWithTrueCondition = new MyFeatureWithTrueActivationCondition();
            var featureWithFalseCondition = new MyFeatureWithFalseActivationCondition();

            var featureSettings = new FeatureActivator(new SettingsHolder());

            featureSettings.Add(featureWithTrueCondition);
            featureSettings.Add(featureWithFalseCondition);


            featureSettings.SetupFeatures(new FeatureConfigurationContext(Configure.With()));

            Assert.True(featureWithTrueCondition.IsActive);
            Assert.False(featureWithFalseCondition.IsActive);
            Assert.AreEqual("The description",
                featureSettings.Status.Single(s => s.Name == featureWithFalseCondition.Name).PrerequisiteStatus.Reasons.First());
        }

        [Test]
        public void Should_register_defaults_if_present()
        {
            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(new MyFeatureWithDefaults());

            featureSettings.SetupFeatures(new FeatureConfigurationContext(Configure.With()));

            Assert.True(settings.Get<bool>("Test1"));
            Assert.True(settings.Get<bool>("Test2"));
        }


        public class MyFeature : TestFeature
        {
        }

        public class MyFeatureWithDefaults : TestFeature
        {
            public MyFeatureWithDefaults()
            {
                Defaults(s => s.SetDefault("Test1", true));
                Defaults(s => s.SetDefault("Test2", true));
            }
        }

        public class MyFeatureWithTrueActivationCondition : TestFeature
        {
            public MyFeatureWithTrueActivationCondition()
            {
                EnableByDefault();
                Prerequisite(c => true,"Wont be used");
            }
        }

        public class MyFeatureWithFalseActivationCondition : TestFeature
        {
            public MyFeatureWithFalseActivationCondition()
            {
                EnableByDefault();
                Prerequisite(c => false,"The description");
            }
        }

    }

    public abstract class TestFeature : Feature
    {
        public bool Enabled
        {
            get { return IsEnabledByDefault; }
            set { if (value) EnableByDefault(); }
        }

        public Action<Feature> OnActivation;

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (OnActivation != null)
            {
                OnActivation(this);
            }
        }
    }
}