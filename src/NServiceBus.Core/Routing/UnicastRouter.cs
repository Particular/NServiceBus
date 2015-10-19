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
        EndpointInstances endpointInstances;
        TransportAddresses physicalAddresses;
        MessageMetadataRegistry messageMetadataRegistry;
        UnicastRoutingTable unicastRoutingTable;

        public UnicastRouter(MessageMetadataRegistry messageMetadataRegistry, UnicastRoutingTable unicastRoutingTable, EndpointInstances endpointInstances, TransportAddresses physicalAddresses)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.unicastRoutingTable = unicastRoutingTable;
            this.endpointInstances = endpointInstances;
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
                e => endpointInstances.FindInstances(e).Select(i => i.Name),
                distributionStrategy.SelectDestination,
                i => physicalAddresses.GetPhysicalAddress(i)
                ));
        }
    }
}