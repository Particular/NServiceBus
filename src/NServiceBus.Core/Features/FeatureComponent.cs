namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Settings;

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

        public Task Start(IMessageSession session)
        {
            messageSession = session;
            featureRunner = new FeatureRunner(featureActivator);
            return featureRunner.Start(container.Builder, messageSession);
        }

        public Task Stop()
        {
            return featureRunner.Stop(messageSession);
        }

        static bool IsFeature(Type type)
        {
            return typeof(Feature).IsAssignableFrom(type);
        }

        SettingsHolder settings;
        ContainerComponent container;
        FeatureActivator featureActivator;
        FeatureRunner featureRunner;
        IMessageSession messageSession;
    }
}