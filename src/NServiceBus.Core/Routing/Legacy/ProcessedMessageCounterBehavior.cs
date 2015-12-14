namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class ProcessedMessageCounterBehavior : Behavior<IncomingPhysicalMessageContext>
    {
        ReadyMessageSender readyMessageSender;

        public ProcessedMessageCounterBehavior(ReadyMessageSender readyMessageSender)
        {
            this.readyMessageSender = readyMessageSender;
        }

        public override async Task Invoke(IncomingPhysicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);
            readyMessageSender.MessageProcessed(context.Message.Headers);
        }
    }
}