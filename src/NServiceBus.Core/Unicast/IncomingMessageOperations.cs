namespace NServiceBus;

using System.Collections.Generic;
using System.Threading.Tasks;
using Pipeline;
using Routing;
using Transport;
using NServiceBus.Utils;

static class IncomingMessageOperations
{
    public static Task ForwardCurrentMessageTo(IIncomingContext context, string destination)
    {
        var messageBeingProcessed = context.Extensions.Get<IncomingMessage>();
        var headers = HeaderPool.Shared.Rent(messageBeingProcessed.Headers.Count);
        messageBeingProcessed.Headers.CopyTo(headers);

        var outgoingMessage = new OutgoingMessage(
                        messageBeingProcessed.MessageId,
                        headers,
                        messageBeingProcessed.Body);

        var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), context);

        return routingContext.InvokePipeline<IRoutingContext>();
    }
}