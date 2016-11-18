namespace NServiceBus.Core.Tests.Features
{
    using System;
    using System.Linq;
    using NServiceBus.Features;
    using Transport;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class FeatureSettingsTests
    {
        private FeatureActivator featureSettings;
        private SettingsHolder settings;

        [SetUp]
        public void Init()
        {
            settings = new SettingsHolder();
            settings.Set<TransportDefinition>(new MsmqTransport());
            featureSettings = new FeatureActivator(settings);
        }

        [Test]
        public void Should_check_prerequisites()
        {
            var featureWithTrueCondition = new MyFeatureWithSatisfiedPrerequisite();
            var featureWithFalseCondition = new MyFeatureWithUnsatisfiedPrerequisite();

            featureSettings.Add(featureWithTrueCondition);
            featureSettings.Add(featureWithFalseCondition);

            featureSettings.SetupFeatures(null, null, null);

            Assert.True(featureWithTrueCondition.IsActive);
            Assert.False(featureWithFalseCondition.IsActive);
            Assert.AreEqual("The description",
                featureSettings.Status.Single(s => s.Name == featureWithFalseCondition.Name).PrerequisiteStatus.Reasons.First());
        }

        [Test]
        public void Should_register_defaults_if_feature_is_activated()
        {
            featureSettings.Add(new MyFeatureWithDefaults());

            featureSettings.SetupFeatures(null, null, null);

            Assert.True(settings.HasSetting("Test1"));
        }

        [Test,Ignore("Discuss if this is possible since pre-requirements can only be checked when settings is locked. And with settings locked we can't register defaults. So there is always a chance that the feature decides to not go ahead with the setup and in that case defaults would already been applied")]
        public void Should_not_register_defaults_if_feature_is_not_activated()
        {
            featureSettings.Add(new MyFeatureWithDefaultsNotActive());
            featureSettings.Add(new MyFeatureWithDefaultsNotActiveDueToUnsatisfiedPrerequisite());

            featureSettings.SetupFeatures(null, null, null);

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
                OnDefaults?.Invoke(this);
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
            OnActivation?.Invoke(this);
        }
    }
}