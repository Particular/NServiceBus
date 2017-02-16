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

            List<string> publisherTransportAddresses = null;
            foreach (var publisherAddress in publishersOfThisEvent)
            {
                if(publisherTransportAddresses == null)
                {
                    publisherTransportAddresses = new List<string>();
                }
                publisherTransportAddresses.AddRange(publisherAddress.Resolve(e => endpointInstances.FindInstances(e), i => transportAddressTranslation(i)));
            }
            return publisherTransportAddresses ?? Enumerable.Empty<string>();
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;

        Publishers publishers;
    }
}