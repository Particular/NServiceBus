namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using OutgoingPipeline;
    using Pipeline;
    using Routing;

    class MulticastPublishRouterBehavior : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        public override Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            var logicalMessageContext = new OutgoingLogicalMessageContext(
                context.MessageId,
                context.Headers,
                context.Message,
                new[]
                {
                    new MulticastRoutingStrategy(context.Message.MessageType)
                },
                context);

            return stage(logicalMessageContext);
        }
    }
}