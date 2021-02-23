namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Transport;

    class MainOnCompleteExecutor
    {
        public MainOnCompleteExecutor(INotificationSubscriptions<ReceiveCompleted> processingCompletedSubscribers) =>
            this.processingCompletedSubscribers = processingCompletedSubscribers;

        public Task Invoke(CompleteContext completeContext, CancellationToken cancellationToken) =>
            processingCompletedSubscribers.Raise(new ReceiveCompleted(completeContext.NativeMessageId, completeContext.WasAcknowledged, completeContext.Headers, completeContext.StartedAt, completeContext.CompletedAt), cancellationToken);

        readonly INotificationSubscriptions<ReceiveCompleted> processingCompletedSubscribers;
    }
}