namespace NServiceBus.AcceptanceTests
{
    using System.Collections.Generic;
    using Configuration.AdvanceExtensibility;
    using Features;
    using NServiceBus.Routing;
    using Settings;

    class EndpointInstancesConfigurationFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Routing.EndpointInstances.AddOrReplaceInstances("testing", context.Settings.GetRegisteredEndpointInstances());
        }
    }

    static class EndpointInstancesConfigurationExtensions
    {
        const string settingsKey = "EndpointInstancesConfigurationExtensions";

        public static void RegisterEndpointInstances(this EndpointConfiguration configuration, params EndpointInstance[] instances)
        {
            configuration.EnableFeature<EndpointInstancesConfigurationFeature>();
            configuration.GetSettings().Set(settingsKey, new List<EndpointInstance>(instances));
        }

        public static List<EndpointInstance> GetRegisteredEndpointInstances(this ReadOnlySettings settings)
        {
            return settings.Get<List<EndpointInstance>>(settingsKey);
        }
    }
}