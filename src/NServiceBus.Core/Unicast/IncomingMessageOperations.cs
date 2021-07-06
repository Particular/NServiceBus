namespace NServiceBus
{
    using System.Collections.Generic;
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
                            new Dictionary<string, string>(messageBeingProcessed.Headers),
                            messageBeingProcessed.Body);

            var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), context);

            return routingContext.InvokePipeline<IRoutingContext>();
        }
    }
}