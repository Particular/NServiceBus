namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Transports;
    using Unicast.Messages;

    abstract class UnicastRouter : IUnicastRouter
    {
        public UnicastRouter(MessageMetadataRegistry messageMetadataRegistry,
            EndpointInstances endpointInstances,
            TransportAddresses physicalAddresses)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.endpointInstances = endpointInstances;
            this.physicalAddresses = physicalAddresses;
        }

        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, DistributionStrategy distributionStrategy, ContextBag contextBag)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType)
                .MessageHierarchy
                .Distinct()
                .ToList();

            var routes = await GetDestinations(contextBag, typesToRoute).ConfigureAwait(false);
            var destinations = new List<UnicastRoutingTarget>();
            foreach (var route in routes)
            {
                destinations.AddRange(await route.Resolve(InstanceResolver).ConfigureAwait(false));
            }

            var destinationsByEndpoint = destinations
                .GroupBy(d => d.Endpoint, d => d);

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionStrategy, destinationsByEndpoint);

            return selectedDestinations
                .Select(destination => destination.Resolve(x => physicalAddresses.GetTransportAddress(new LogicalAddress(x))))
                .Distinct() //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
                .Select(destination => new UnicastRoutingStrategy(destination));
        }

        protected abstract Task<IEnumerable<IUnicastRoute>> GetDestinations(ContextBag contextBag, List<Type> typesToRoute);

        Task<IEnumerable<EndpointInstance>> InstanceResolver(EndpointName endpoint)
        {
            return endpointInstances.FindInstances(endpoint);
        }

        static IEnumerable<UnicastRoutingTarget> SelectDestinationsForEachEndpoint(DistributionStrategy distributionStrategy, IEnumerable<IGrouping<EndpointName, UnicastRoutingTarget>> destinationsByEndpoint)
        {
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
                    foreach (var destination in distributionStrategy.SelectDestination(@group))
                    {
                        yield return destination;
                    }
                }
            }
        }

        EndpointInstances endpointInstances;
        MessageMetadataRegistry messageMetadataRegistry;
        TransportAddresses physicalAddresses;
    }
}