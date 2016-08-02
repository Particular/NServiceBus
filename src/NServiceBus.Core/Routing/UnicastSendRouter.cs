namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Unicast.Messages;

    class UnicastSendRouter : UnicastRouter
    {
        public UnicastSendRouter(MessageMetadataRegistry messageMetadataRegistry, UnicastRoutingTable unicastRoutingTable, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation)
            : base(messageMetadataRegistry, endpointInstances, transportAddressTranslation)
        {
            this.unicastRoutingTable = unicastRoutingTable;
        }

        protected override Task<IEnumerable<IUnicastRoute>> GetDestinations(ContextBag contextBag, Type[] typesToRoute)
        {
            return unicastRoutingTable.GetDestinationsFor(typesToRoute, contextBag);
        }

        UnicastRoutingTable unicastRoutingTable;
    }
}