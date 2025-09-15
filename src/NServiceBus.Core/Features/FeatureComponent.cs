#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Settings;

class FeatureComponent(SettingsHolder settings)
{
    public void RegisterFeatureEnabledStatusInSettings(HostingComponent.Configuration hostingConfiguration)
    {
        var featureSettings = settings.Get<Settings>();
        foreach (var type in featureSettings.Features.Keys)
        {
            featureActivator.Add(type.Construct<Feature>());
        }

        foreach (var type in hostingConfiguration.AvailableTypes.Where(IsFeature))
        {
            if (!featureSettings.Features.ContainsKey(type))
            {
                featureActivator.Add(type.Construct<Feature>());
            }
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

    public class Settings
    {
        readonly SettingsHolder settings;

        public Settings(SettingsHolder settings)
        {
            this.settings = settings;
            Features = [];
        }

        public Dictionary<Type, (FeatureState? Default, FeatureState? Override)> Features
        {
            get => settings.Get<Dictionary<Type, (FeatureState? Default, FeatureState? Override)>>("NServiceBus.Features.Features");
            private init => settings.Set("NServiceBus.Features.Features", value);
        }
    }
}