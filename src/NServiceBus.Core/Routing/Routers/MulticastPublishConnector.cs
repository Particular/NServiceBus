namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    class MulticastPublishConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        public override Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> stage, CancellationToken cancellationToken)
        {
            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(
                context.Message,
                new[]
                {
                    new MulticastRoutingStrategy(context.Message.MessageType)
                },
                context);

            return stage(logicalMessageContext, cancellationToken);
        }
    }
}