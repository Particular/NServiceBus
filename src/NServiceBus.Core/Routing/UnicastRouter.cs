namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;

    class UnicastRouter : IUnicastRouter
    {
        EndpoointInstances endpoointInstances;
        TransportAddresses physicalAddresses;
        MessageMetadataRegistry messageMetadataRegistry;
        UnicastRoutingTable unicastRoutingTable;

        public UnicastRouter(MessageMetadataRegistry messageMetadataRegistry, UnicastRoutingTable unicastRoutingTable, EndpoointInstances endpoointInstances, TransportAddresses physicalAddresses)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.unicastRoutingTable = unicastRoutingTable;
            this.endpoointInstances = endpoointInstances;
            this.physicalAddresses = physicalAddresses;
        }

        public IEnumerable<UnicastRoutingStrategy> Route(Type messageType, DistributionStrategy distributionStrategy, ContextBag contextBag)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType)
                .MessageHierarchy
                .Distinct()
                .ToList();

            var destinationEndpoints = typesToRoute.SelectMany(t => unicastRoutingTable.GetDestinationsFor(t, contextBag)).Distinct().ToList();

            return destinationEndpoints.SelectMany(d => d.Resolve(
                e => endpoointInstances.FindInstances(e),
                distributionStrategy.SelectDestination,
                i => physicalAddresses.GetPhysicalAddress(i)
                )).Select(a => new UnicastRoutingStrategy(a));
        }
    }
}