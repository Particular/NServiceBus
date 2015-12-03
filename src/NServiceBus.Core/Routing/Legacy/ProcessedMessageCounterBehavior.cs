namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class ProcessedMessageCounterBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        ReadyMessageSender readyMessageSender;

        public ProcessedMessageCounterBehavior(ReadyMessageSender readyMessageSender)
        {
            this.readyMessageSender = readyMessageSender;
        }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            await next();
            readyMessageSender.MessageProcessed(context.Message.Headers);
        }
    }
}