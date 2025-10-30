namespace NServiceBus.Core.Tests.Features;

using System;
using System.Linq;
using NServiceBus.Features;
using NUnit.Framework;
using Settings;

[TestFixture]
public class FeatureSettingsTests
{
    FeatureComponent.Settings featureSettings;
    FeatureComponent featureComponent;
    SettingsHolder settings;
    FakeFeatureFactory featureFactory;

    [SetUp]
    public void Init()
    {
        settings = new SettingsHolder();
        featureFactory = new FakeFeatureFactory();
        featureSettings = new FeatureComponent.Settings(featureFactory);
        settings.Set(featureSettings);
        featureComponent = new FeatureComponent(featureSettings);
    }

    [Test]
    public void Should_check_prerequisites()
    {
        var featureWithTrueCondition = new MyFeatureWithSatisfiedPrerequisite();
        var featureWithFalseCondition = new MyFeatureWithUnsatisfiedPrerequisite();

        featureFactory.Add(featureWithTrueCondition, featureWithFalseCondition);

        featureSettings.EnableFeatureByDefault<MyFeatureWithSatisfiedPrerequisite>();
        featureSettings.EnableFeatureByDefault<MyFeatureWithUnsatisfiedPrerequisite>();

        var status = featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureWithTrueCondition.IsActive, Is.True);
            Assert.That(featureWithFalseCondition.IsActive, Is.False);
            Assert.That(status.Single(s => s.Name == featureWithFalseCondition.Name).PrerequisiteStatus.Reasons.First(),
                Is.EqualTo("The description"));
        }
    }

    [Test]
    public void Should_register_defaults_if_feature_is_activated()
    {
        featureSettings.EnableFeatureByDefault<MyFeatureWithDefaults>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        Assert.That(settings.HasSetting("Test1"), Is.True);
    }

    public class MyFeatureWithDefaults : TestFeature
    {
        public MyFeatureWithDefaults() => Defaults(s => s.SetDefault("Test1", true));
    }

    public class MyFeatureWithDefaultsNotActive : TestFeature
    {
        public MyFeatureWithDefaultsNotActive() => Defaults(s => s.SetDefault("Test1", true));
    }

    public class MyFeatureWithDefaultsNotActiveDueToUnsatisfiedPrerequisite : TestFeature
    {
        public MyFeatureWithDefaultsNotActiveDueToUnsatisfiedPrerequisite()
        {
            Defaults(s => s.SetDefault("Test2", true));
            Prerequisite(c => false, "Not to be activated");
        }
    }

    public class MyFeatureWithSatisfiedPrerequisite : TestFeature
    {
        public MyFeatureWithSatisfiedPrerequisite() => Prerequisite(c => true, "Wont be used");
    }

    public class MyFeatureWithUnsatisfiedPrerequisite : TestFeature
    {
        public MyFeatureWithUnsatisfiedPrerequisite() => Prerequisite(c => false, "The description");
    }

}

public abstract class TestFeature : Feature
{
    protected TestFeature() =>
        Defaults(s =>
        {
            OnDefaults?.Invoke(this);
        });

    public bool Enabled
    {
        get => IsEnabledByDefault;
        set
        {
            if (value)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                EnableByDefault();
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
    }

    public Action<Feature> OnActivation;
    public Action<Feature> OnDefaults;

    protected internal override void Setup(FeatureConfigurationContext context) => OnActivation?.Invoke(this);
}