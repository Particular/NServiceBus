#nullable enable

namespace NServiceBus;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Settings;

class FeatureComponent(SettingsHolder settings)
{
    public void RegisterFeatureEnabledStatusInSettings(HostingComponent.Configuration hostingConfiguration)
    {
        foreach (var type in hostingConfiguration.AvailableTypes.Where(IsFeature))
        {
            featureActivator.Add(type.Construct<Feature>());
        }
    }

    public void Initialize(FeatureConfigurationContext featureConfigurationContext)
    {
        var featureStats = featureActivator.SetupFeatures(featureConfigurationContext);

        settings.AddStartupDiagnosticsSection("Features", featureStats);
    }

    public Task Start(IServiceProvider builder, IMessageSession messageSession, CancellationToken cancellationToken = default) => featureActivator.StartFeatures(builder, messageSession, cancellationToken);

    public Task Stop(IMessageSession messageSession, CancellationToken cancellationToken = default) => featureActivator.StopFeatures(messageSession, cancellationToken);

    static bool IsFeature(Type type) => typeof(Feature).IsAssignableFrom(type);

    readonly FeatureActivator featureActivator = new(settings);
}