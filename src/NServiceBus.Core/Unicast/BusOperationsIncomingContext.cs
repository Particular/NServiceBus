namespace NServiceBus.Unicast
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    static class BusOperationsIncomingContext
    {
        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        public static async Task ForwardCurrentMessageToAsync(IncomingContext context, string destination)
        {
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

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), context);

            await pipeline.Invoke(routingContext).ConfigureAwait(false);
        }
    }
}