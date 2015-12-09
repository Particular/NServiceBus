namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing.Legacy;

    class ProcessedMessageCounterBehavior : Behavior<IncomingPhysicalMessageContext>
    {
        ReadyMessageSender readyMessageSender;

        public ProcessedMessageCounterBehavior(ReadyMessageSender readyMessageSender)
        {
            this.readyMessageSender = readyMessageSender;
        }

        public override async Task Invoke(IncomingPhysicalMessageContext context, Func<Task> next)
        {
            await next();
            readyMessageSender.MessageProcessed(context.Message.Headers);
        }
    }
}