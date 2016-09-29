namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class NoOpCanceling : ICancelDeferredMessages
    {
        public Task CancelDeferredMessages(string messageKey, IBehaviorContext context)
        {
            //no-op
            return TaskEx.CompletedTask;
        }
    }
}