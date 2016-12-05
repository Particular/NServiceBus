namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using Configuration.AdvanceExtensibility;
    using Routing;
    using Routing.MessageDrivenSubscriptions;

    static class UnicastRoutingExtensions
    {
        public static void RegisterPublisher(this RoutingSettings config, Type eventType, string publisherEndpoint)
        {
            config.GetSettings().GetOrCreate<Publishers>().AddOrReplacePublishers(Guid.NewGuid().ToString(),
                new List<PublisherTableEntry> { new PublisherTableEntry(eventType, PublisherAddress.CreateFromEndpointName(publisherEndpoint))});
        }

        public static void RegisterEndpointInstances(this RoutingSettings config, params EndpointInstance[] instances)
        {
            config.GetSettings().GetOrCreate<EndpointInstances>().AddOrReplaceInstances(Guid.NewGuid().ToString(), instances);
        }
    }
}