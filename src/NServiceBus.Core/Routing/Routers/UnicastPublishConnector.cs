namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Unicast.Queuing;

    class UnicastPublishConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        public UnicastPublishConnector(IUnicastPublishRouter unicastPublishRouter, DistributionPolicy distributionPolicy)
        {
            this.unicastPublishRouter = unicastPublishRouter;
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

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            try
            {
                await stage(this.CreateOutgoingLogicalMessageContext(context.Message, addressLabels, context)).ConfigureAwait(false);
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