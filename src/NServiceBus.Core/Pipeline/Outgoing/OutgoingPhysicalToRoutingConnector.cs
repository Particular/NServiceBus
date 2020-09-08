namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class OutgoingPhysicalToRoutingConnector : StageConnector<IOutgoingPhysicalMessageContext, IRoutingContext>
    {
        public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<IRoutingContext, CancellationToken, Task> stage, CancellationToken cancellationToken)
        {
            var message = new OutgoingMessage(context.MessageId, context.Headers, context.Body);

            return stage(this.CreateRoutingContext(message, context.RoutingStrategies, context), cancellationToken);
        }
    }
}