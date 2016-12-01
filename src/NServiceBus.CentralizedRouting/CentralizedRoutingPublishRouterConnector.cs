namespace NServiceBus.CentralizedRouting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Unicast.Messages;
    using Unicast.Queuing;

    class CentralizedRoutingPublishRouterConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        MessageMetadataRegistry messageMetadataRegistry;
        CentralizedPubSubRoutingTable routingTable;
        DistributionPolicy distributionPolicy;
        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;

        public CentralizedRoutingPublishRouterConnector(MessageMetadataRegistry messageMetadataRegistry, CentralizedPubSubRoutingTable routingTable, DistributionPolicy distributionPolicy, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.routingTable = routingTable;
            this.distributionPolicy = distributionPolicy;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var eventType = context.Message.MessageType;
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(eventType).MessageHierarchy;

            var subscribers = routingTable.GetSubscribersFor(typesToRoute);

            var selectedDestinations = SelectDestinationsForEachEndpoint(subscribers);

            var strategies = selectedDestinations.Select(destination => new UnicastRoutingStrategy(destination)).ToArray();

            if (strategies.Length == 0)
            {
                //No subscribers for this message.
                return;
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            try
            {
                await stage(this.CreateOutgoingLogicalMessageContext(context.Message, strategies, context)).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({eventType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn\'t been created yet, or has been deleted.", ex);
            }
        }

        IEnumerable<string> SelectDestinationsForEachEndpoint(IEnumerable<string> subscribers)
        {
            return subscribers.Select(SelectDestinationForEndpoint);
        }

        string SelectDestinationForEndpoint(string endpoint)
        {
            var distributionStrategy = distributionPolicy.GetDistributionStrategy(endpoint, DistributionStrategyScope.Send);
            var instances = endpointInstances.FindInstances(endpoint).Select(transportAddressTranslation).ToArray();
            return distributionStrategy.SelectReceiver(instances);
        }
    }
}