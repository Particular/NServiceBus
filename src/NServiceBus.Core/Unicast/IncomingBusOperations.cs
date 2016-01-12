namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    static class IncomingBusOperations
    {
        public static async Task ForwardCurrentMessageTo(IIncomingContext context, string destination)
        {
            var messageBeingProcessed = context.Extensions.Get<IncomingMessage>();

            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IRoutingContext>();

            var outgoingMessage = new OutgoingMessage(
                messageBeingProcessed.MessageId,
                messageBeingProcessed.Headers,
                messageBeingProcessed.Body);

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), context);

            await pipeline.Invoke(routingContext).ConfigureAwait(false);
        }
    }
}