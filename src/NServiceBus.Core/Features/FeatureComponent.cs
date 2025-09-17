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

        // When scanning is enabled we might find some types multiple times so we need to de-dupe them.
        var featureTypes = new HashSet<Type>(featureSettings.Features);

        // When scanning is disabled the available types might only contain explicitely added stuff.
        foreach (var type in hostingConfiguration.AvailableTypes.Where(IsFeature))
        {
            featureTypes.Add(type);
        }

        foreach (var featureType in featureTypes)
        {
            featureActivator.Add(featureType);
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

    readonly FeatureActivator featureActivator = new(settings, new FeatureFactory());

    public class Settings
    {
        readonly SettingsHolder settings;

        public Settings(SettingsHolder settings)
        {
            this.settings = settings;
            Features = [];
        }

        public HashSet<Type> Features
        {
            get => settings.Get<HashSet<Type>>("NServiceBus.Features.Features");
            private init => settings.Set("NServiceBus.Features.Features", value);
        }
    }
}