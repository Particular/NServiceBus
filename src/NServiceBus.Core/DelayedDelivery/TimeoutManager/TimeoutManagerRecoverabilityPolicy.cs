namespace NServiceBus
{
    using Transport;

    class TimeoutManagerRecoverabilityPolicy : IRecoverabilityPolicy
    {
        public TimeoutManagerRecoverabilityPolicy()
        {
            RaiseRecoverabilityNotifications = false;
        }

        public RecoverabilityAction Invoke(ErrorContext errorContext)
        {
            if (errorContext.NumberOfDeliveryAttempts <= MaxNumberOfFailedRetries)
            {
                return RecoverabilityAction.ImmediateRetry();
            }

            return RecoverabilityAction.MoveToError();
        }

        public bool RaiseRecoverabilityNotifications { get; }

        const int MaxNumberOfFailedRetries = 4;
    }
}