namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

        public List<string> GetAddressesForEventType(Type messageType)
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
            return publisherTransportAddresses ?? noAddresses;
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        static List<string> noAddresses = new List<string>(0);

        Publishers publishers;
    }
}