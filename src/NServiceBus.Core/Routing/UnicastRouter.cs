namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Unicast.Messages;

    abstract class UnicastRouter : IUnicastRouter
    {
        protected UnicastRouter(MessageMetadataRegistry messageMetadataRegistry,
            EndpointInstances endpointInstances,
            Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, ContextBag contextBag)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType).MessageHierarchy;

            var routes = await GetDestinations(contextBag, typesToRoute).ConfigureAwait(false);
            var destinations = new HashSet<UnicastRoutingTarget>();
            foreach (var route in routes)
            {
                var routingTargets = await route.Resolve(InstanceResolver).ConfigureAwait(false);
                foreach (var routingTarget in routingTargets)
                {
                    destinations.Add(routingTarget);
                }
            }

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionPolicy, destinations);

            return selectedDestinations
                .Select(destination => destination.Resolve(x => transportAddressTranslation(x)))
                .Distinct() //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
                .Select(destination => new UnicastRoutingStrategy(destination));
        }

        protected abstract Task<IEnumerable<IUnicastRoute>> GetDestinations(ContextBag contextBag, Type[] types);

        Task<IEnumerable<EndpointInstance>> InstanceResolver(string endpoint)
        {
            return endpointInstances.FindInstances(endpoint);
        }

        static IEnumerable<UnicastRoutingTarget> SelectDestinationsForEachEndpoint(IDistributionPolicy distributionPolicy, HashSet<UnicastRoutingTarget> destinations)
        {
            var destinationsByEndpoint = destinations
                .GroupBy(d => d.Endpoint, d => d);

            foreach (var group in destinationsByEndpoint)
            {
                if (@group.Key == null) //Routing targets that do not specify endpoint name
                {
                    //Send a message to each target as we have no idea which endpoint they represent
                    foreach (var destination in @group)
                    {
                        yield return destination;
                    }
                }
                else
                {
                    //Use the distribution strategy to select subset of instances of a given endpoint
                    foreach (var destination in distributionPolicy.GetDistributionStrategy(group.Key).SelectDestination(@group.ToArray()))
                    {
                        yield return destination;
                    }
                }
            }
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        MessageMetadataRegistry messageMetadataRegistry;
    }
}