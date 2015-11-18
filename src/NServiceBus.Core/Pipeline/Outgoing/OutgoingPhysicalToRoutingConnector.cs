namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;
    using TransportDispatch;
    using Transports;

    class OutgoingPhysicalToRoutingConnector : StageConnector<OutgoingPhysicalMessageContext, RoutingContext>
    {
        public override Task Invoke(OutgoingPhysicalMessageContext context, Func<RoutingContext, Task> next)
        {
            context.Headers[Headers.MessageId] = context.MessageId;

            var message = new OutgoingMessage(context.MessageId, context.Headers, context.Body);

            return next(new RoutingContext(message, context.RoutingStrategies, context));
        }
    }
}