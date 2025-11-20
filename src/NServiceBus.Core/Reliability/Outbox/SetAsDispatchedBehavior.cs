namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Outbox;
using Pipeline;

class SetAsDispatchedBehavior(IOutboxStorage outboxStorage)
    : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
{
    public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
    {
        if (!context.Message.Headers.TryGetValue("NServiceBus.Outbox.SetAsDispatched", out var messageId))
        {
            return next(context);
        }

        return outboxStorage.SetAsDispatched(messageId, context.Extensions, context.CancellationToken);
    }
}