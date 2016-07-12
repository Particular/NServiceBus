namespace NServiceBus
{
    using System;
    using Logging;

    class DefaultRecoverabilityPolicy : IRecoverabilityPolicy
    {
        public DefaultRecoverabilityPolicy(bool immediateRetriesEnabled, bool delayedRetriesEnabled, int maxImmediateRetries, SecondLevelRetryPolicy secondLevelRetryPolicy)
        {
            this.immediateRetriesEnabled = immediateRetriesEnabled;
            this.delayedRetriesEnabled = delayedRetriesEnabled;
            this.maxImmediateRetries = maxImmediateRetries;
            this.secondLevelRetryPolicy = secondLevelRetryPolicy;

            RaiseRecoverabilityNotifications = true;
        }

        public RecoverabilityAction Invoke(ErrorContext errorContext)
        {
            if (immediateRetriesEnabled)
            {
                if (errorContext.NumberOfDeliveryAttempts <= maxImmediateRetries)
                {
                    return RecoverabilityAction.ImmediateRetry();
                }

                Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", errorContext.Message.MessageId);
            }

            if (delayedRetriesEnabled)
            {
                var slrRetryContext = new SecondLevelRetryContext
                {
                    ExceptionInfo = errorContext.ExceptionInfo,
                    Message = errorContext.Message,
                    SecondLevelRetryAttempt = errorContext.Message.GetCurrentDelayedRetries() + 1
                };

                TimeSpan retryDelay;
                if (secondLevelRetryPolicy.TryGetDelay(slrRetryContext, out retryDelay))
                {
                    return RecoverabilityAction.DelayedRetry(retryDelay);
                }

                Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", errorContext.Message.MessageId);
            }

            return RecoverabilityAction.MoveToError();
        }

        public bool RaiseRecoverabilityNotifications { get; }

        bool immediateRetriesEnabled;
        bool delayedRetriesEnabled;
        int maxImmediateRetries;
        SecondLevelRetryPolicy secondLevelRetryPolicy;

        static ILog Logger = LogManager.GetLogger<DefaultRecoverabilityPolicy>();
    }
}