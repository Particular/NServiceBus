namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using Settings;

    class FeatureComponent
    {
        public FeatureComponent(SettingsHolder settings)
        {
            this.settings = settings;
        }

        // concreteTypes will be some kind of TypeDiscoveryComponent when we encapsulate the scanning
        public void Initalize(List<Type> concreteTypes, ContainerComponent containerComponent, FeatureConfigurationContext featureConfigurationContext)
        {
            container = containerComponent;

            featureActivator = new FeatureActivator(settings);

            foreach (var type in concreteTypes.Where(t => IsFeature(t)))
            {
                featureActivator.Add(type.Construct<Feature>());
            }

            var featureStats = featureActivator.SetupFeatures(featureConfigurationContext);

            settings.AddStartupDiagnosticsSection("Features", featureStats);
        }

        public Task Start(IMessageSession messageSession)
        {
            return featureActivator.StartFeatures(container.Builder, messageSession);
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
        ContainerComponent container;
    }
}