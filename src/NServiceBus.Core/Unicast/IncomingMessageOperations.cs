namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transport;

    static class IncomingMessageOperations
    {
        public static Task ForwardCurrentMessageTo(IIncomingContext context, string destination)
        {
            var messageBeingProcessed = context.Extensions.Get<IncomingMessage>();
            var outgoingMessage = new OutgoingMessage(
                            messageBeingProcessed.MessageId,
                            messageBeingProcessed.Headers,
                            messageBeingProcessed.Body);

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), context);

            // TODO: Temporary use of CancellationToken.None
            return routingContext.InvokePipeline<IRoutingContext>(CancellationToken.None);
        }
    }
}