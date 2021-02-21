namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Transport;

    class MainOnCompleteExecutor
    {
        public MainOnCompleteExecutor(INotificationSubscriptions<ProcessingCompleted> processingCompletedSubscribers) =>
            this.processingCompletedSubscribers = processingCompletedSubscribers;

        public Task Invoke(CompleteContext completeContext, CancellationToken cancellationToken) =>
            processingCompletedSubscribers.Raise(new ProcessingCompleted(completeContext.MessageId, completeContext.WasAcknowledged, completeContext.Headers, completeContext.StartedAt, completeContext.CompletedAt), cancellationToken);

        readonly INotificationSubscriptions<ProcessingCompleted> processingCompletedSubscribers;
    }
}