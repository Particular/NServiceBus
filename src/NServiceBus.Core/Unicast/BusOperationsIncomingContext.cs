namespace NServiceBus.Unicast
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    static class BusOperationsIncomingContext
    {
        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        public static async Task ForwardCurrentMessageTo(IPipeInlet<RoutingContext> pipeline, IncomingContext context, string destination)
        {
            var messageBeingProcessed = context.Get<IncomingMessage>();

            var outgoingMessage = new OutgoingMessage(
                messageBeingProcessed.MessageId,
                messageBeingProcessed.Headers,
                messageBeingProcessed.Body);

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), context);

            await pipeline.Put(routingContext).ConfigureAwait(false);
        }
    }
}