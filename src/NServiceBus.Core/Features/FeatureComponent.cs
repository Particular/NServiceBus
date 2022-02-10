namespace NServiceBus
{
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
        }

        public void RegisterFeatureEnabledStatusInSettings(HostingComponent.Configuration hostingConfiguration)
        {
            featureActivator = new FeatureActivator(settings);

            // also register features that haven't been scanned via assembly scanning but have been explicitly configured via code first APIs.
            var registeredFeatureTypes = settings.GetExplicitlyEnabledFeatures();

            foreach (var type in hostingConfiguration.AvailableTypes
                         .Union(registeredFeatureTypes)
                         .Where(t => IsFeature(t))
                         .Distinct())
            {
                featureActivator.Add(type.Construct<Feature>());
            }
        }

        public void Initalize(FeatureConfigurationContext featureConfigurationContext)
        {
            var featureStats = featureActivator.SetupFeatures(featureConfigurationContext);

            settings.AddStartupDiagnosticsSection("Features", featureStats);
        }

        public Task Start(IServiceProvider builder, IMessageSession messageSession, CancellationToken cancellationToken = default)
        {
            return featureActivator.StartFeatures(builder, messageSession, cancellationToken);
        }

        public Task Stop(CancellationToken cancellationToken = default)
        {
            return featureActivator.StopFeatures(cancellationToken);
        }

        static bool IsFeature(Type type) => typeof(Feature).IsAssignableFrom(type);

        SettingsHolder settings;
        FeatureActivator featureActivator;
    }
}