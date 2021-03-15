namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Transport;

    class SubscriptionRouter
    {
        public SubscriptionRouter(Publishers publishers, EndpointInstances endpointInstances, TransportInfrastructure transportInfrastructure)
        {
            this.publishers = publishers;
            this.endpointInstances = endpointInstances;
            this.transportInfrastructure = transportInfrastructure;
        }

        public List<string> GetAddressesForEventType(Type messageType)
        {
            var publishersOfThisEvent = publishers.GetPublisherFor(messageType);

            List<string> publisherTransportAddresses = null;
            foreach (var publisherAddress in publishersOfThisEvent)
            {
                if (publisherTransportAddresses == null)
                {
                    publisherTransportAddresses = new List<string>();
                }
                publisherTransportAddresses.AddRange(publisherAddress.Resolve(e => endpointInstances.FindInstances(e), i => transportInfrastructure.ToTransportAddress(new QueueAddress(i.Endpoint, i.Discriminator, i.Properties, null))));
            }
            return publisherTransportAddresses ?? noAddresses;
        }

        EndpointInstances endpointInstances;
        readonly TransportInfrastructure transportInfrastructure;
        static List<string> noAddresses = new List<string>(0);

        Publishers publishers;
    }
}