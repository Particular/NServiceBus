namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Unicast.Queuing;

    class UnicastSendRouterConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        public UnicastSendRouterConnector(UnicastSend.PhysicalRouter physicalRouter, UnicastSend.LogicalRouter logicalRouter)
        {
            this.physicalRouter = physicalRouter;
            this.logicalRouter = logicalRouter;
        }

        public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var routingStrategy = physicalRouter.Route(context);

            if (routingStrategy == null)
            {
                routingStrategy = logicalRouter.Route(context);

                if (routingStrategy == null)
                {
                    throw new Exception($"No destination specified for message: {context.Message.MessageType}");
                }
            }
            
            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Send.ToString();

            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(context.Message,new[]{ routingStrategy }, context);

            try
            {
                await stage(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({context.Message.MessageType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn't been created yet, or has been deleted.", ex);
            }
        }

        UnicastSend.LogicalRouter logicalRouter;
        UnicastSend.PhysicalRouter physicalRouter;
    }
}