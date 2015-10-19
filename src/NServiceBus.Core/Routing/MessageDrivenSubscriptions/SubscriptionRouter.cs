namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Transports;

    class SubscriptionRouter
    {
        public SubscriptionRouter(Publishers publishers, EndpointInstances knownEndpoints, TransportAddresses physicalAddresses)
        {
            this.publishers = publishers;
            this.knownEndpoints = knownEndpoints;
            this.physicalAddresses = physicalAddresses;
        }

        public IEnumerable<string> GetAddressesForEventType(Type messageType)
        {
            var publisherAddresses = publishers
                .GetPublisherFor(messageType).SelectMany(p => p
                    .Resolve(e => knownEndpoints.FindInstances(e).Select(i => i.Name), i => physicalAddresses.GetPhysicalAddress(i)));

            return publisherAddresses;
        }

        Publishers publishers;
        EndpointInstances knownEndpoints;
        TransportAddresses physicalAddresses;
    }
}