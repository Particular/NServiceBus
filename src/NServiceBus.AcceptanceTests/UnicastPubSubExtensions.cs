namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Collections.Generic;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;

    static class UnicastPubSubExtensions
    {
        public static void RegisterPublisher(this EndpointConfiguration config, Type eventType, string publisherEndpoint)
        {
            config.GetSettings().GetOrCreate<Publishers>().AddOrReplacePublishers(Guid.NewGuid(), 
                new List<PublisherTableEntry> { new PublisherTableEntry(eventType, PublisherAddress.CreateFromEndpointName(publisherEndpoint))});
        }
    }
}