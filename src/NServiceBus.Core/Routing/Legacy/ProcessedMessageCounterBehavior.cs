namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ProcessedMessageCounterBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public ProcessedMessageCounterBehavior(ReadyMessageSender readyMessageSender)
        {
            this.readyMessageSender = readyMessageSender;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);
            await readyMessageSender.MessageProcessed(context.Message.Headers).ConfigureAwait(false);
        }

        ReadyMessageSender readyMessageSender;
    }
}