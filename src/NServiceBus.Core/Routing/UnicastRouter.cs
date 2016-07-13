namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Transport;
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

        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, ContextBag contextBag)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType)
                .MessageHierarchy
                .Distinct()
                .ToArray();

            var routes = await GetDestinations(contextBag, typesToRoute).ConfigureAwait(false);

            var destinations = new List<UnicastRoutingTarget>();
            foreach (var route in routes)
            {
                destinations.AddRange(await ResolveInstances(route).ConfigureAwait(false));
            }

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionPolicy, destinations);

            return selectedDestinations
                .Select(destination => destination.Resolve(x => physicalAddresses.GetTransportAddress(x)))
                .Distinct() //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
                .Select(destination => new UnicastRoutingStrategy(destination));
        }

        async Task<UnicastRoutingTarget[]> ResolveInstances(UnicastRoute route)
        {
            if (route.PhysicalAddress != null)
            {
                return new[]
                {
                    UnicastRoutingTarget.ToTransportAddress(route.PhysicalAddress)
                };
            }
            else if (route.Instance != null)
            {
                return new[]
                {
                    UnicastRoutingTarget.ToEndpointInstance(route.Instance)
                };
            }
            else
            {
                var instances = await endpointInstances.FindInstances(route.Endpoint).ConfigureAwait(false);
                return instances.Select(UnicastRoutingTarget.ToEndpointInstance).ToArray();
            }
        }

        protected abstract Task<List<UnicastRoute>> GetDestinations(ContextBag contextBag, Type[] typesToRoute);

        static IEnumerable<UnicastRoutingTarget> SelectDestinationsForEachEndpoint(IDistributionPolicy distributionPolicy, List<UnicastRoutingTarget> destinations)
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
        MessageMetadataRegistry messageMetadataRegistry;
        TransportAddresses physicalAddresses;
    }
}