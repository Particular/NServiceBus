namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;
    using OutgoingPipeline;
    using Pipeline;

    class MulticastPublishRouterBehavior : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        public override Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(
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