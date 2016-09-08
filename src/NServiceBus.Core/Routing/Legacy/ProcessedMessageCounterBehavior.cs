namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ProcessedMessageCounterBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public ProcessedMessageCounterBehavior(ReadyMessageSender readyMessageSender)
        {
            this.readyMessageSender = readyMessageSender;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            await next(context).ConfigureAwait(false);
            await readyMessageSender.MessageProcessed(context.Message.Headers).ConfigureAwait(false);
        }

        ReadyMessageSender readyMessageSender;
    }
}