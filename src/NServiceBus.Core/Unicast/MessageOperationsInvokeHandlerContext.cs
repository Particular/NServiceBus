namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Pipeline;
    using Routing;
    using Settings;
    using Transport;

    static class MessageOperationsInvokeHandlerContext
    {
        public static Task HandleCurrentMessageLater(IInvokeHandlerContext context)
        {
            if (context.HandleCurrentMessageLaterWasCalled)
            {
                return TaskEx.CompletedTask;
            }

            var messageBeingProcessed = context.Extensions.Get<IncomingMessage>();
            var settings = context.Builder.Build<ReadOnlySettings>();

            if (settings.IsFeatureActive(typeof(Features.Outbox)))
            {
                // as HandleCurrentMessageLater reuses the incoming message's message id, this will cause the message to be deduplicated by the outbox causing a message loss.
                throw new InvalidOperationException("HandleCurrentMessageLater cannot be used in conjunction with the Outbox. Use the recoverability mechanisms or delayed delivery instead.");
            }

            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IRoutingContext>();

            var outgoingMessage = new OutgoingMessage(
                messageBeingProcessed.MessageId,
                messageBeingProcessed.Headers,
                messageBeingProcessed.Body);

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(settings.LocalAddress()), context);

            return pipeline.Invoke(routingContext);
        }
    }
}