namespace NServiceBus.Core.Tests.Features;

using NServiceBus.Features;
using NUnit.Framework;
using Settings;

[TestFixture]
public class FeatureStateTests
{
    FeatureRegistry featureSettings;
    SettingsHolder settings;

    [SetUp]
    public void Init()
    {
        settings = new SettingsHolder();
        featureSettings = new FeatureRegistry(settings, new FeatureFactory());
        settings.Set(featureSettings);
    }

    [Test]
    public void Should_deactivate_when_dependency_not_meet_due_to_prerequisite()
    {
        featureSettings.EnableFeature<FeatureThatDependsOnAnother>();
        featureSettings.EnableFeature<DependentFeature>();

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

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

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

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

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

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

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Active), Is.True);
        }
    }

    [Test]
    public void Should_activate_features_that_are_enabled_by_default()
    {
        featureSettings.EnableFeatureByDefault<FeatureThatGetsToggled>();

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Active), Is.True);
        }
    }

    [Test]
    public void Should_enable_features_that_are_enabled()
    {
        featureSettings.EnableFeature<FeatureThatGetsToggled>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Enabled), Is.True);
        }
    }

    [Test]
    public void Should_enable_features_that_are_enabled_by_default()
    {
        featureSettings.EnableFeatureByDefault<FeatureThatGetsToggled>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Enabled), Is.True);
        }
    }

    [Test]
    public void Should_disable_features_that_are_disabled()
    {
        featureSettings.DisableFeature<FeatureThatGetsToggled>();

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Disabled), Is.True);
        }
    }

    [Test]
    public void Default_state_is_disabled()
    {
        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(featureSettings.IsFeature<FeatureThatGetsToggled>(FeatureState.Disabled), Is.True);
        }
    }

    sealed class FeatureThatGetsToggled : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}