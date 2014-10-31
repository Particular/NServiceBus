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
        public void Should_check_prerequisites()
        {
            var featureWithTrueCondition = new MyFeatureWithSatisfiedPrerequisite();
            var featureWithFalseCondition = new MyFeatureWithUnsatisfiedPrerequisite();

            var featureSettings = new FeatureActivator(new SettingsHolder());

            featureSettings.Add(featureWithTrueCondition);
            featureSettings.Add(featureWithFalseCondition);


            featureSettings.SetupFeatures(new FeatureConfigurationContext(null));

            Assert.True(featureWithTrueCondition.IsActive);
            Assert.False(featureWithFalseCondition.IsActive);
            Assert.AreEqual("The description",
                featureSettings.Status.Single(s => s.Name == featureWithFalseCondition.Name).PrerequisiteStatus.Reasons.First());
        }

        [Test]
        public void Should_register_defaults_if_feature_is_activated()
        {
            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(new MyFeatureWithDefaults());

            featureSettings.SetupFeatures(new FeatureConfigurationContext(null));

            Assert.True(settings.HasSetting("Test1"));
        }

        [Test,Ignore("We need to discuss if this is possible since prereqs can only be checked when settings is locked. And with settings locked we can't register defaults. So there is always a chance that the feature decides to not go ahead with the setup and in that case defaults would already been applied")]
        public void Should_not_register_defaults_if_feature_is_not_activated()
        {
            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(new MyFeatureWithDefaultsNotActive());
            featureSettings.Add(new MyFeatureWithDefaultsNotActiveDueToUnsatisfiedPrerequisite());

            featureSettings.SetupFeatures(new FeatureConfigurationContext(null));

            Assert.False(settings.HasSetting("Test1"));
            Assert.False(settings.HasSetting("Test2"));
        }


        public class MyFeature : TestFeature
        {
        }

        public class MyFeatureWithDefaults : TestFeature
        {
            public MyFeatureWithDefaults()
            {
                EnableByDefault();
                Defaults(s => s.SetDefault("Test1", true));
            }
        }

        public class MyFeatureWithDefaultsNotActive : TestFeature
        {
            public MyFeatureWithDefaultsNotActive()
            {
                Defaults(s => s.SetDefault("Test1", true));
            }
        }

        public class MyFeatureWithDefaultsNotActiveDueToUnsatisfiedPrerequisite : TestFeature
        {
            public MyFeatureWithDefaultsNotActiveDueToUnsatisfiedPrerequisite()
            {
                EnableByDefault();
                Defaults(s => s.SetDefault("Test2", true));
                Prerequisite(c => false, "Not to be activated");
            }
        }

        public class MyFeatureWithSatisfiedPrerequisite : TestFeature
        {
            public MyFeatureWithSatisfiedPrerequisite()
            {
                EnableByDefault();
                Prerequisite(c => true, "Wont be used");
            }
        }

        public class MyFeatureWithUnsatisfiedPrerequisite : TestFeature
        {
            public MyFeatureWithUnsatisfiedPrerequisite()
            {
                EnableByDefault();
                Prerequisite(c => false, "The description");
            }
        }

    }

    public abstract class TestFeature : Feature
    {
        protected TestFeature()
        {
            Defaults(s =>
            {
                if (OnDefaults != null)
                {
                    OnDefaults(this);
                }
            });
        }

        public bool Enabled
        {
            get { return IsEnabledByDefault; }
            set { if (value) EnableByDefault(); }
        }

        public Action<Feature> OnActivation;
        public Action<Feature> OnDefaults;

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (OnActivation != null)
            {
                OnActivation(this);
            }
        }
    }
}