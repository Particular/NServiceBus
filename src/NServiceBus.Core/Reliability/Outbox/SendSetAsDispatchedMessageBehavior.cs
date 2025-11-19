namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipeline;
using Routing;
using Transport;

class SendSetAsDispatchedMessageBehavior(
    QueueAddress localQueueAddress,
    ITransportAddressResolver transportAddressResolver)
    : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
{
    readonly string localAddress = transportAddressResolver.ToTransportAddress(localQueueAddress);

    public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
    {
        var pendingOps = context.Extensions.Get<PendingTransportOperations>();
        var headers = new Dictionary<string, string>
        {
            ["NServiceBus.Outbox.SetAsDispatched"] = context.MessageId
        };
        var cleanupMessage = new OutgoingMessage(CombGuid.Generate().ToString(), headers, Array.Empty<byte>());
        pendingOps.Add(new TransportOperation(cleanupMessage, new UnicastAddressTag(localAddress)));

        return next(context);
    }
}