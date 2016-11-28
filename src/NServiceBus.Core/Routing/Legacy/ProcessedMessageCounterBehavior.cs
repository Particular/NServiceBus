namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class ProcessedMessageCounterBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public ProcessedMessageCounterBehavior(ReadyMessageSender readyMessageSender, NotificationSubscriptions subscriptions)
        {
            this.readyMessageSender = readyMessageSender;

            subscriptions.Subscribe<MessageFaulted>(HandleMessageFaulted);
            subscriptions.Subscribe<MessageToBeRetried>(HandleMessageRetried);
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            await next(context).ConfigureAwait(false);
            await readyMessageSender.MessageProcessed(context.Message.Headers, context.Extensions.GetOrCreate<TransportTransaction>()).ConfigureAwait(false);
        }

        Task HandleMessageFaulted(MessageProcessingFailed @event)
        {
            return readyMessageSender.MessageProcessed(@event.Message.Headers, @event.ErrorContext.TransportTransaction);
        }

        Task HandleMessageRetried(MessageToBeRetried @event)
        {
            return @event.IsImmediateRetry ? TaskEx.CompletedTask : readyMessageSender.MessageProcessed(@event.Message.Headers, @event.ErrorContext.TransportTransaction);
        }

        ReadyMessageSender readyMessageSender;
    }
}