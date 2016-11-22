namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transport;

    static class IncomingMessageOperations
    {
        public static Task ForwardCurrentMessageTo(IIncomingContext context, string destination)
        {
            var messageBeingProcessed = context.Extensions.Get<IncomingMessage>();

            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IRoutingContext>();

            var outgoingMessage = messageBeingProcessed.ToOutgoingMessage();

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), context);

            return pipeline.Invoke(routingContext);
        }
    }
}