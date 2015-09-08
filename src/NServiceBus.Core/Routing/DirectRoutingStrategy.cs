namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;

    class DirectRoutingStrategy : IDirectRoutingStrategy
    {
        KnownEndpoints knownEndpoints;
        TransportAddresses physicalAddresses;
        MessageMetadataRegistry messageMetadataRegistry;
        DirectRoutingTable directRoutingTable;

        public DirectRoutingStrategy(MessageMetadataRegistry messageMetadataRegistry, DirectRoutingTable directRoutingTable, KnownEndpoints knownEndpoints, TransportAddresses physicalAddresses)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.directRoutingTable = directRoutingTable;
            this.knownEndpoints = knownEndpoints;
            this.physicalAddresses = physicalAddresses;
        }

        public IEnumerable<AddressLabel> Route(Type messageType, DistributionStrategy distributionStrategy, ContextBag contextBag)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType)
                .MessageHierarchy
                .Distinct()
                .ToList();

            var destinationEndpoints = typesToRoute.SelectMany(t => directRoutingTable.GetDestinationsFor(t, contextBag)).Distinct().ToList();

            return destinationEndpoints.SelectMany(d => d.Resolve(
                e => knownEndpoints.FindInstances(e),
                distributionStrategy.SelectDestination,
                i => physicalAddresses.GetPhysicalAddress(i)
                )).Select(a => new DirectAddressLabel(a));
        }
    }
}