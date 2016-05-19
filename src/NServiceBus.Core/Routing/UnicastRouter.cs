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
        public UnicastRouter(
            string name,
            MessageMetadataRegistry messageMetadataRegistry,
            EndpointInstances endpointInstances,
            TransportAddresses physicalAddresses,
            DistributionPolicy distributionPolicy,
            List<Type> allMessageTypes)
        {
            this.name = name;
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.endpointInstances = endpointInstances;
            this.physicalAddresses = physicalAddresses;
            this.distributionPolicy = distributionPolicy;
            this.allMessageTypes = allMessageTypes;
        }

        protected abstract Task<IEnumerable<IUnicastRoute>> GetDestinationsFor(List<Type> messageTypeHierarchy);

        public async Task RebuildRoutingTable()
        {
            routingTable = await UnicastRoutingTable.Build(name, GetDestinationsFor, endpointInstances, distributionPolicy, allMessageTypes, messageMetadataRegistry).ConfigureAwait(false);
        }

        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, ContextBag contextBag)
        {
            // We lazy-load the tables because there is no hook to invoke stuff after all features have been set up but before the message session is initialized and passed to FSTs
            if (routingTable == null)
            {
                await RebuildRoutingTable().ConfigureAwait(false);
            }

            // RebuildRoutingTable ensures routing table is initialized.
            // ReSharper disable once PossibleNullReferenceException
            var targets = routingTable.Route(messageType, contextBag);

            return targets
                .Select(destination => destination.Resolve(x => physicalAddresses.GetTransportAddress(new LogicalAddress(x))))
                .Distinct() //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
                .Select(destination => new UnicastRoutingStrategy(destination));
        }

        EndpointInstances endpointInstances;
        string name;
        MessageMetadataRegistry messageMetadataRegistry;
        TransportAddresses physicalAddresses;
        DistributionPolicy distributionPolicy;
        List<Type> allMessageTypes;
        UnicastRoutingTable routingTable;
    }
}