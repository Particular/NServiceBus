namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            var results = new List<string>();
            foreach (var publisherAddress in publishers.GetPublisherFor(messageType))
            {
                results.AddRange(await publisherAddress.Resolve(
                    ResolveInstances,
                    i => transportAddressTranslation(i)).ConfigureAwait(false));
            }
            return results.Distinct();
        }

        Task<IEnumerable<EndpointInstance>> ResolveInstances(string endpoint)
        {
            return endpointInstances.FindInstances(endpoint);
        }

        EndpointInstances endpointInstances;
        readonly Func<EndpointInstance, string> transportAddressTranslation;

        Publishers publishers;
    }
}