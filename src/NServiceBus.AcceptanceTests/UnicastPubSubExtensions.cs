namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Routing.MessageDrivenSubscriptions;

    static class UnicastPubSubExtensions
    {
        public static void RegisterPublisherForType(this EndpointConfiguration config, string publisherEndpoint, Type eventType)
        {
            config.GetSettings().GetOrCreate<Publishers>().Add(publisherEndpoint, eventType);
        }
    }
}