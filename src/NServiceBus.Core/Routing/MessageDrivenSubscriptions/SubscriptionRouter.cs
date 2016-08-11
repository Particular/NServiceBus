namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Routing;
    using Routing.MessageDrivenSubscriptions;

    class SubscriptionRouter
    {
        public SubscriptionRouter(Publishers publishers, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.publishers = publishers;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public async Task<IEnumerable<string>> GetAddressesForEventType(Type messageType)
        {
            var publisher = publishers.GetPublisherFor(messageType);
            var publisherTransportAddresses = await publisher.Resolve(e => endpointInstances.FindInstances(e), i => transportAddressTranslation(i)).ConfigureAwait(false);
            return publisherTransportAddresses;
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;

        Publishers publishers;
    }
}