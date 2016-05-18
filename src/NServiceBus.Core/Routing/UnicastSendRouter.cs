namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Transports;
    using Unicast.Messages;

    class UnicastSendRouter : UnicastRouter
    {
        public UnicastSendRouter(MessageMetadataRegistry messageMetadataRegistry, UnicastRoutingTable unicastRoutingTable, EndpointInstances endpointInstances, TransportAddresses physicalAddresses) 
            : base(messageMetadataRegistry, endpointInstances, physicalAddresses)
        {
            this.unicastRoutingTable = unicastRoutingTable;
        }

        protected override Task<IEnumerable<IUnicastRoute>> GetDestinations(ContextBag contextBag, List<Type> typesToRoute)
        {
            return unicastRoutingTable.GetDestinationsFor(typesToRoute, contextBag);
        }

        UnicastRoutingTable unicastRoutingTable;
    }
}