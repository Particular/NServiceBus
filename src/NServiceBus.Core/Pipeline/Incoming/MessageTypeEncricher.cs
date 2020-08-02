namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using Pipeline;

    class MessageTypeEnricher : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public async Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            if (!context.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                context.Headers[Headers.EnclosedMessageTypes] = context.Message.MessageType.FullName;
            }

            await next(context).ConfigureAwait(false);
        }
    }
}