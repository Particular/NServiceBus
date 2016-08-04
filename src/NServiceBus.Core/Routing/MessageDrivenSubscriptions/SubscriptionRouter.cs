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
            var results = new HashSet<string>();
            foreach (var publisherAddress in publishers.GetPublisherFor(messageType))
            {
                var addresses = await publisherAddress.Resolve(endpoint => endpointInstances.FindInstances(endpoint), i => transportAddressTranslation(i)).ConfigureAwait(false);
                foreach (var address in addresses)
                {
                    results.Add(address);
                }
            }

            return results;
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;

        Publishers publishers;
    }
}