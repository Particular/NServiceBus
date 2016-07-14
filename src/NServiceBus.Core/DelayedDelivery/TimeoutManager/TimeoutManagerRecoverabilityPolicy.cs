namespace NServiceBus
{
    using Transport;

    class TimeoutManagerRecoverabilityPolicy
    {
        public static RecoverabilityAction Invoke(RecoverabilityConfig config, ErrorContext errorContext)
        {
            if (errorContext.NumberOfImmediateDeliveryAttempts <= MaxNumberOfFailedRetries)
            {
                return RecoverabilityAction.ImmediateRetry();
            }

            return RecoverabilityAction.MoveToError();
        }

        const int MaxNumberOfFailedRetries = 4;
    }
}