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

            foreach (var type in hostingConfiguration.AvailableTypes.Where(t => IsFeature(t)))
            {
                featureActivator.Add(type.Construct<Feature>());
            }
        }

        public async Task Initalize(FeatureConfigurationContext featureConfigurationContext, CancellationToken cancellationToken = default)
        {
            var featureStats = await featureActivator.SetupFeatures(featureConfigurationContext, cancellationToken).ConfigureAwait(false);

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

        static bool IsFeature(Type type)
        {
            return typeof(Feature).IsAssignableFrom(type);
        }

        SettingsHolder settings;
        FeatureActivator featureActivator;
    }
}