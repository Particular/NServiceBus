#nullable enable

namespace NServiceBus;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Settings;

class FeatureComponent
{
    public FeatureComponent(SettingsHolder settings)
    {
        this.settings = settings;
        featureRegistry = new FeatureRegistry(settings, new FeatureFactory());
        this.settings.Set(featureRegistry);
        this.settings.Set(this);
    }

    public void RegisterFeatureEnabledStatusInSettings(HostingComponent.Configuration hostingConfiguration)
    {
        foreach (var featureType in hostingConfiguration.AvailableTypes.Where(IsFeature))
        {
            featureRegistry.Add(featureType);
        }
    }

    public void Initialize(FeatureConfigurationContext featureConfigurationContext)
    {
        var featureStats = featureRegistry.SetupFeatures(featureConfigurationContext);

        settings.AddStartupDiagnosticsSection("Features", featureStats);
    }

    public Task Start(IServiceProvider builder, IMessageSession messageSession, CancellationToken cancellationToken = default) => featureRegistry.StartFeatures(builder, messageSession, cancellationToken);

    public Task Stop(IMessageSession messageSession, CancellationToken cancellationToken = default) => featureRegistry.StopFeatures(messageSession, cancellationToken);

    static bool IsFeature(Type type) => typeof(Feature).IsAssignableFrom(type);

    readonly FeatureRegistry featureRegistry;
    readonly SettingsHolder settings;
}