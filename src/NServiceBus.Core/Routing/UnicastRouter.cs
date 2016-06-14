namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Transports;
    using Unicast.Messages;

    /// <summary>
    /// Implements routing behavior
    ///  * Which logical endpoints should receive a given message,
    ///  * How to map these logical endpoints to physical endpoint instances,
    ///  * Which physical endpoint instances should receive the message,
    ///  * How to map physical endpoint instances to transport-level addresses.
    /// </summary>
    public abstract class UnicastRouter : IUnicastRouter
    {
        /// <summary>
        /// Creates new instance of the router.
        /// </summary>
        protected UnicastRouter(MessageMetadataRegistry messageMetadataRegistry,
            EndpointInstances endpointInstances,
            TransportAddresses physicalAddresses)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.endpointInstances = endpointInstances;
            this.physicalAddresses = physicalAddresses;
        }

        /// <summary>
        /// Determines the destinations for a given message type.
        /// </summary>
        /// <param name="messageType">Type of message.</param>
        /// <param name="distributionPolicy">Distribution policy.</param>
        /// <param name="contextBag">Context.</param>
        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, Func<string, DistributionStrategy> distributionPolicy, ContextBag contextBag)
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

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionPolicy, destinations);

            return selectedDestinations
                .Select(destination => destination.Resolve(x => physicalAddresses.GetTransportAddress(new LogicalAddress(x))))
                .Distinct() //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
                .Select(destination => new UnicastRoutingStrategy(destination));
        }

        /// <summary>
        /// Determines routes for a given message.
        /// </summary>
        /// <param name="contextBag">Context.</param>
        /// <param name="typesToRoute">All message types associated with this message.</param>
        protected abstract Task<IEnumerable<IUnicastRoute>> GetDestinations(ContextBag contextBag, List<Type> typesToRoute);

        Task<IEnumerable<EndpointInstance>> InstanceResolver(string endpoint)
        {
            return endpointInstances.FindInstances(endpoint);
        }

        static IEnumerable<UnicastRoutingTarget> SelectDestinationsForEachEndpoint(Func<string, DistributionStrategy> distributationPolicy, List<UnicastRoutingTarget> destinations)
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
                    foreach (var destination in distributationPolicy(group.Key).SelectDestination(@group.ToArray()))
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