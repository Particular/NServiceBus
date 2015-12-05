namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    static class BusOperationsInvokeHandlerContext
    {
        /// <summary>
        /// Moves the message being handled to the back of the list of available 
        /// messages so it can be handled later.
        /// </summary>
        public static async Task HandleCurrentMessageLater(InvokeHandlerContext context)
        {
            if (context.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            var messageBeingProcessed = context.Get<IncomingMessage>();
            var settings = context.Builder.Build<ReadOnlySettings>();

            var pipeline = new PipelineBase<RoutingContext>(
                context.Builder,
                settings,
                settings.Get<PipelineConfiguration>().MainPipeline);

            var outgoingMessage = new OutgoingMessage(
                messageBeingProcessed.MessageId,
                messageBeingProcessed.Headers,
                messageBeingProcessed.Body);

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(settings.LocalAddress()), context);

            await pipeline.Invoke(routingContext).ConfigureAwait(false);

            context.handleCurrentMessageLaterWasCalled = true;
            context.DoNotContinueDispatchingCurrentMessageToHandlers();
        }
    }
}