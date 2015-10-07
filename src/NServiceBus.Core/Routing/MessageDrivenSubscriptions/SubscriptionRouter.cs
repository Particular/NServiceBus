namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Transports;

    class SubscriptionRouter
    {
        public SubscriptionRouter(Publishers publishers, EndpoointInstances endpoointInstances, TransportAddresses physicalAddresses)
        {
            this.publishers = publishers;
            this.endpoointInstances = endpoointInstances;
            this.physicalAddresses = physicalAddresses;
        }

        public IEnumerable<string> GetAddressesForEventType(Type messageType)
        {
            var publisherAddresses = publishers
                .GetPublisherFor(messageType).SelectMany(p => p
                    .Resolve(e => endpoointInstances.FindInstances(e), i => physicalAddresses.GetPhysicalAddress(i)));

            return publisherAddresses;
        }

        Publishers publishers;
        EndpoointInstances endpoointInstances;
        TransportAddresses physicalAddresses;
    }
}