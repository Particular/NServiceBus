namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Routing;

    static class UnicastRoutingExtensions
    {
        public static void RegisterEndpointInstances(this RoutingSettings config, params EndpointInstance[] instances)
        {
            config.GetSettings().GetOrCreate<EndpointInstances>().AddOrReplaceInstances(Guid.NewGuid().ToString(), instances);
        }
    }
}