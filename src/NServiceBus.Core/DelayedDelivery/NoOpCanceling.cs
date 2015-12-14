namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class NoOpCanceling : ICancelDeferredMessages
    {
        public Task CancelDeferredMessages(string messageKey, IBehaviorContext context)
        {
            //no-op
            return TaskEx.Completed;
        }
    }
}