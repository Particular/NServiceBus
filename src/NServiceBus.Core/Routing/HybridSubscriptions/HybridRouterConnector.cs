namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Unicast.Queuing;

    class HybridRouterConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        public HybridRouterConnector(DistributionPolicy distributionPolicy, IUnicastPublishRouter unicastPublishRouter)
        {
            this.distributionPolicy = distributionPolicy;
            this.unicastPublishRouter = unicastPublishRouter;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            var eventType = context.Message.MessageType;
            var addressLabels = await GetRoutingStrategies(context, eventType).ConfigureAwait(false);

            var routingStrategies = new List<RoutingStrategy>
            {
                new MulticastRoutingStrategy(context.Message.MessageType)
            };

            routingStrategies.AddRange(addressLabels);

            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(
                context.Message,
                routingStrategies,
                context);

            try
            {
                await stage(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({context.Message.MessageType}) in the routing section of the transport configuration. It may also be the case that the given queue hasn\'t been created yet, or has been deleted.", ex);
            }
        }

        async Task<List<UnicastRoutingStrategy>> GetRoutingStrategies(IOutgoingPublishContext context, Type eventType)
        {
            var addressLabels = await unicastPublishRouter.Route(eventType, distributionPolicy, context).ConfigureAwait(false);
            return addressLabels.ToList();
        }

        readonly DistributionPolicy distributionPolicy;
        readonly IUnicastPublishRouter unicastPublishRouter;
    }
}