namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Contexts;
    using OutgoingPipeline;
    using Pipeline;
    using Routing;
    using TransportDispatch;

    class MulticastPublishRouterConnector : StageConnector<OutgoingPublishContext, OutgoingLogicalMessageContext>
    {
        public override Task Invoke(OutgoingPublishContext context, Func<OutgoingLogicalMessageContext, Task> next)
        {
            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Publish.ToString());
            return next(new OutgoingLogicalMessageContext(context.Message, new [] { new MulticastRoutingStrategy(context.Message.MessageType) }, context));
        }
    }
}