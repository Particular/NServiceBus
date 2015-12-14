namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Transports;

    class SubscriptionRouter
    {
        public SubscriptionRouter(Publishers publishers, EndpointInstances endpointInstances, TransportAddresses physicalAddresses)
        {
            this.publishers = publishers;
            this.endpointInstances = endpointInstances;
            this.physicalAddresses = physicalAddresses;
        }

        public IEnumerable<string> GetAddressesForEventType(Type messageType)
        {
            return publishers
                .GetPublisherFor(messageType).SelectMany(p => p
                    .Resolve(e => endpointInstances.FindInstances(e), i => physicalAddresses.GetTransportAddress(i)));
        }

        Publishers publishers;
        EndpointInstances endpointInstances;
        TransportAddresses physicalAddresses;
    }
}