namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class OutgoingPhysicalToRoutingConnector : StageConnector<IOutgoingPhysicalMessageContext, IRoutingContext>
    {
        public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<IRoutingContext, Task> stage)
        {
            context.Headers[Headers.MessageId] = context.MessageId;

            var message = new OutgoingMessage(context.MessageId, context.Headers, context.Body);

            return stage(this.CreateRoutingContext(message, context.RoutingStrategies, context));
        }
    }
}