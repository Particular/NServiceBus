namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Unicast.Queuing;

    class UnicastPublishRouterConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        public UnicastPublishRouterConnector(IUnicastPublishRouter unicastPublishRouter, DistributionPolicy distributionPolicy)
        {
            this.unicastPublishRouter = unicastPublishRouter;
            this.distributionPolicy = distributionPolicy;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var eventType = context.Message.MessageType;
            var routingStrategies = unicastPublishRouter.Route(eventType, distributionPolicy, context.Extensions).ToList();
            if (routingStrategies.Count == 0)
            {
                //No subscribers for this message.
                return;
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            try
            {
                await stage(this.CreateOutgoingLogicalMessageContext(context.Message, routingStrategies, context)).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({eventType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn\'t been created yet, or has been deleted.", ex);
            }
        }

        DistributionPolicy distributionPolicy;
        IUnicastPublishRouter unicastPublishRouter;
    }
}