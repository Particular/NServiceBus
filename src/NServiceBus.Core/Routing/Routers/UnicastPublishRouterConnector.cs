namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Queuing;

    class UnicastPublishRouterConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        IUnicastRouter unicastRouter;
        DistributionPolicy distributionPolicy;
        public UnicastPublishRouterConnector(IUnicastRouter unicastRouter, DistributionPolicy distributionPolicy)
        {
            this.unicastRouter = unicastRouter;
            this.distributionPolicy = distributionPolicy;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var eventType = context.Message.MessageType;
            var addressLabels = await GetRoutingStrategies(context, eventType).ConfigureAwait(false);
            if (addressLabels.Count == 0)
            {
                //No subscribers for this message.
                return;
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Send.ToString();

            try
            {
                await stage(this.CreateOutgoingLogicalMessageContext(context.Message, addressLabels, context)).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. You may have misconfigured the destination for this kind of message ({eventType}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex);
            }
        }

        async Task<List<UnicastRoutingStrategy>> GetRoutingStrategies(IOutgoingPublishContext context, Type eventType)
        {
            var distributionStrategy = distributionPolicy.GetDistributionStrategy(eventType);
            var addressLabels = await unicastRouter.Route(eventType, distributionStrategy, context.Extensions).ConfigureAwait(false);
            return addressLabels.ToList();
        }
    }
}