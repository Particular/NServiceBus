namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        public IEnumerable<string> GetAddressesForEventType(Type messageType)
        {
            var publishersOfThisEvent = publishers.GetPublisherFor(messageType);
            var publisherTransportAddresses = publishersOfThisEvent.SelectMany(p => p.Resolve(e => endpointInstances.FindInstances(e), i => transportAddressTranslation(i)));
            return publisherTransportAddresses;
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;

        Publishers publishers;
    }
}