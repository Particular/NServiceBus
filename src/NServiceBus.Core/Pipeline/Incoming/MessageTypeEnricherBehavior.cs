namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class MessageTypeEnricherBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            if (!context.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                context.Headers[Headers.EnclosedMessageTypes] = context.Message.MessageType.FullName;
            }

            await next().ConfigureAwait(false);
        }
    }
}