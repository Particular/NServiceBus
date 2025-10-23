namespace NServiceBus.Core.Tests.Features;

using NServiceBus.Features;
using NUnit.Framework;
using Settings;

[TestFixture]
public class FeatureStateTests
{
    FeatureComponent.Settings featureSettings;
    FeatureComponent featureComponent;
    SettingsHolder settings;

    [SetUp]
    public void Init()
    {
        settings = new SettingsHolder();

        featureSettings = new FeatureComponent.Settings();
        settings.Set(featureSettings);
        featureComponent = new FeatureComponent(featureSettings);
    }

    [Test]
    public void Should_deactivate_when_dependency_not_meet_due_to_prerequisite()
    {
        featureSettings.EnableFeature<FeatureThatDependsOnAnother>();
        featureSettings.EnableFeature<DependentFeature>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureSettings.IsFeature<FeatureThatDependsOnAnother>(FeatureState.Deactivated), Is.True);
            Assert.That(featureSettings.IsFeature<DependentFeature>(FeatureState.Deactivated), Is.True);
        }
    }

    [Test]
    public void Should_deactivate_when_dependency_not_meet()
    {
        featureSettings.EnableFeature<FeatureThatDependsOnAnother>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureSettings.IsFeature<FeatureThatDependsOnAnother>(FeatureState.Deactivated), Is.True);
            Assert.That(featureSettings.IsFeature<DependentFeature>(FeatureState.Disabled), Is.True);
        }
    }

    [Test]
    public void Should_deactivate_when_dependency_not_meet_due_to_disabling()
    {
        featureSettings.EnableFeature<FeatureThatDependsOnAnother>();
        featureSettings.DisableFeature<DependentFeature>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureSettings.IsFeature<FeatureThatDependsOnAnother>(FeatureState.Deactivated), Is.True);
            Assert.That(featureSettings.IsFeature<DependentFeature>(FeatureState.Disabled), Is.True);
        }
    }

    sealed class FeatureThatDependsOnAnother : Feature
    {
        public FeatureThatDependsOnAnother() => DependsOn<DependentFeature>();

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }

    sealed class DependentFeature : Feature
    {
        public DependentFeature() => Prerequisite(c => false, "Not to be activated");

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }

    [Test]
    public void Should_activate_features_that_are_enabled()
    {
        featureSettings.EnableFeature<FeatureThatGetsToggled>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Active), Is.True);
    }

    [Test]
    public void Should_activate_features_that_are_enabled_by_default()
    {
        featureSettings.EnableFeatureByDefault<FeatureThatGetsToggled>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Active), Is.True);
    }

    [Test]
    public void Should_enable_features_that_are_enabled()
    {
        featureSettings.EnableFeature<FeatureThatGetsToggled>();

        Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Enabled), Is.True);
    }

    [Test]
    public void Should_not_enable_features_that_are_enabled_by_default()
    {
        featureSettings.EnableFeatureByDefault<FeatureThatGetsToggled>();

        Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Enabled), Is.False);
    }

    [Test]
    public void Should_disable_features_that_are_disabled()
    {
        featureSettings.DisableFeature<FeatureThatGetsToggled>();

        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Disabled), Is.True);
    }

    [Test]
    public void Default_state_is_disabled()
    {
        featureComponent.SetupFeatures(new FakeFeatureConfigurationContext(), settings);

        Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Disabled), Is.True);
    }

    sealed class FeatureThatGetsToggled : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}