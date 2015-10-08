namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            var publisherAddresses = publishers
                .GetPublisherFor(messageType).SelectMany(p => p
                    .Resolve(e => endpointInstances.FindInstances(e), i => physicalAddresses.GetPhysicalAddress(i)));

            return publisherAddresses;
        }

        Publishers publishers;
        EndpointInstances endpointInstances;
        TransportAddresses physicalAddresses;
    }
}