namespace NServiceBus.AcceptanceTests
{
    using System.Collections.Generic;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Routing;
    using Settings;

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