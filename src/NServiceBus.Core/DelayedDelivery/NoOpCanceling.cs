namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class NoOpCanceling : ICancelDeferredMessages
    {
        public Task CancelDeferredMessages(string messageKey, IBehaviorContext context)
        {
            //no-op
            return TaskEx.CompletedTask;
        }
    }
}