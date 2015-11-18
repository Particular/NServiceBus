namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using OutgoingPipeline;
    using Pipeline;
    using Routing;

    class MulticastPublishRouterBehavior : StageConnector<OutgoingPublishContext, OutgoingLogicalMessageContext>
    {
        public override Task Invoke(OutgoingPublishContext context, Func<OutgoingLogicalMessageContext, Task> next)
        {
            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();
            return next(new OutgoingLogicalMessageContext(
                context.MessageId,
                context.Headers,
                context.Message, 
                new [] { new MulticastRoutingStrategy(context.Message.MessageType) }, 
                context));
        }
    }
}