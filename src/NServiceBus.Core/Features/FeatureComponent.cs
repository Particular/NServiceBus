namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using ObjectBuilder;
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

        public void Initalize(FeatureConfigurationContext featureConfigurationContext)
        {
            var featureStats = featureActivator.SetupFeatures(featureConfigurationContext);

            settings.AddStartupDiagnosticsSection("Features", featureStats);
        }

        public Task Start(IBuilder builder, IMessageSession messageSession)
        {
            return featureActivator.StartFeatures(builder, messageSession);
        }

        public Task Stop()
        {
            return featureActivator.StopFeatures();
        }

        static bool IsFeature(Type type)
        {
            return typeof(Feature).IsAssignableFrom(type);
        }

        SettingsHolder settings;
        FeatureActivator featureActivator;
    }
}