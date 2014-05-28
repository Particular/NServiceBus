namespace NServiceBus.Core.Tests.Features
{
    using System;
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
       

            featureSettings.SetupFeatures();

            Assert.True(featureWithTrueCondition.IsActive);
            Assert.False(featureWithFalseCondition.IsActive);
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