namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Routing;
    using Routing.MessageDrivenSubscriptions;

    class SubscriptionRouter
    {
        public SubscriptionRouter(Publishers publishers, EndpointInstances endpointInstances, Func<EndpointInstance, CancellationToken, Task<string>> transportAddressTranslation)
        {
            this.publishers = publishers;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public async Task<List<string>> GetAddressesForEventType(Type messageType, CancellationToken cancellationToken = default)
        {
            var publishersOfThisEvent = publishers.GetPublisherFor(messageType);

            List<string> publisherTransportAddresses = null;
            foreach (var publisherAddress in publishersOfThisEvent)
            {
                if (publisherTransportAddresses == null)
                {
                    publisherTransportAddresses = new List<string>();
                }
                publisherTransportAddresses.AddRange(await publisherAddress.Resolve(e => endpointInstances.FindInstances(e), (i, token) => transportAddressTranslation(i, token), cancellationToken).ConfigureAwait(false));
            }
            return publisherTransportAddresses ?? noAddresses;
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, CancellationToken, Task<string>> transportAddressTranslation;
        static List<string> noAddresses = new List<string>(0);

        Publishers publishers;
    }
}