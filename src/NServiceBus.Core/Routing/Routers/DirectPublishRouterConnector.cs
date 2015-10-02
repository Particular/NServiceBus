namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Unicast.Queuing;

    class DirectPublishRouterConnector : StageConnector<OutgoingPublishContext, OutgoingLogicalMessageContext>
    {
        DirectRoutingStrategy directRoutingStrategy;
        DistributionPolicy distributionPolicy;

        public DirectPublishRouterConnector(DirectRoutingStrategy directRoutingStrategy, DistributionPolicy distributionPolicy)
        {
            this.directRoutingStrategy = directRoutingStrategy;
            this.distributionPolicy = distributionPolicy;
        }

        public override async Task Invoke(OutgoingPublishContext context, Func<OutgoingLogicalMessageContext, Task> next)
        {
            var eventType = context.Message.MessageType;
            var distributionStrategy = distributionPolicy.GetDistributionStrategy(eventType);

            var addressLabels = directRoutingStrategy.Route(eventType, distributionStrategy, context)
                .EnsureNonEmpty(() => "No destination specified for message: " + eventType)
                .ToArray();

            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Send.ToString());
            try
            {
                await next(new OutgoingLogicalMessageContext(context.Message, addressLabels, context)).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. You may have misconfigured the destination for this kind of message ({eventType}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex);
            }
        }
    }
}