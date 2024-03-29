namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class InferredMessageTypeEnricherBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
{
    public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
    {
        if (!context.Headers.ContainsKey(Headers.EnclosedMessageTypes))
        {
            context.Headers[Headers.EnclosedMessageTypes] = context.Message.MessageType.FullName;
        }

        return next(context);
    }
}