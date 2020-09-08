namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class NoOpCanceling : ICancelDeferredMessages
    {
        public Task CancelDeferredMessages(string messageKey, IBehaviorContext context, CancellationToken cancellationToken)
        {
            //no-op
            return Task.CompletedTask;
        }
    }
}