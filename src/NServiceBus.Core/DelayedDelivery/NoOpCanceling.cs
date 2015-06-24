namespace NServiceBus.DelayedDelivery
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class NoOpCanceling : ICancelDeferredMessages
    {
        public void CancelDeferredMessages(string messageKey, BehaviorContext context)
        {
            //no-op       
        }
    }
}