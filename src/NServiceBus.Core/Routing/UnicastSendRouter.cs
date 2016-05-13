namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Routing;
    using Transports;
    using Unicast.Messages;

    class UnicastSendRouter : UnicastRouter
    {
        public UnicastSendRouter(string name, MessageMetadataRegistry messageMetadataRegistry, UnicastRoutingTableConfiguration routingTableConfiguration, EndpointInstances endpointInstances, TransportAddresses physicalAddresses, DistributionPolicy distributionPolicy, List<Type> allMessageTypes) 
            : base(name, messageMetadataRegistry, endpointInstances, physicalAddresses, distributionPolicy, allMessageTypes)
        {
            this.routingTableConfiguration = routingTableConfiguration;
        }

        protected override Task<IEnumerable<IUnicastRoute>> GetDestinationsFor(List<Type> messageTypeHierarchy)
        {
            return routingTableConfiguration.GetDestinationsFor(messageTypeHierarchy);
        }

        UnicastRoutingTableConfiguration routingTableConfiguration;
    }
}