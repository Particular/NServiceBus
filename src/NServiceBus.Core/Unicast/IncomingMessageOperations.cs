namespace NServiceBus;

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Routing;
using Transport;
using NServiceBus.Utils;

static class IncomingMessageOperations
{
    public static Task ForwardCurrentMessageTo(IIncomingContext context, string destination)
    {
        var messageBeingProcessed = context.Extensions.Get<IncomingMessage>();
        var headerPool = context.Builder.GetRequiredService<HeaderPool>();
        var headers = headerPool.Rent(messageBeingProcessed.Headers.Count);
        messageBeingProcessed.Headers.CopyTo(headers);

        var outgoingMessage = new OutgoingMessage(
                        messageBeingProcessed.MessageId,
                        headers,
                        messageBeingProcessed.Body);

        var routingContext = new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(destination), context);

        return routingContext.InvokePipeline<IRoutingContext>();
    }
}