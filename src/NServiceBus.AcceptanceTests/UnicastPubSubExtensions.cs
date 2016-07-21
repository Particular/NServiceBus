namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Routing.MessageDrivenSubscriptions;

    static class UnicastPubSubExtensions
    {
        public static void RegisterPublisher(this EndpointConfiguration config, Type eventType, string publisherEndpoint)
        {
            config.GetSettings().GetOrCreate<Publishers>().Add(eventType, publisherEndpoint);
        }
    }
}