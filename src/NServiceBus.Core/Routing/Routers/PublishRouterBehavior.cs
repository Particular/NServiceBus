namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Unicast.Queuing;

    class PublishRouterBehavior : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        IPublishRouter publishRouter;

        public PublishRouterBehavior(IPublishRouter publishRouter)
        {
            this.publishRouter = publishRouter;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            IReadOnlyCollection<RoutingStrategy> routingStrategies = await publishRouter.GetRoutingStrategies(context).ConfigureAwait(false);
            if (routingStrategies.Count == 0)
            {
                return;
            }

            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(context.Message, routingStrategies, context);

            try
            {
                await stage(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({context.Message.MessageType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn\'t been created yet, or has been deleted.", ex);
            }
        }
    }
}