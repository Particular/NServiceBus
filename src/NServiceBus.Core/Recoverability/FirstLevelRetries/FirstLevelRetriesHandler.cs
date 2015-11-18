namespace NServiceBus.Recoverability.FirstLevelRetries
{
    using NServiceBus.Logging;
    using NServiceBus.Transports;

    internal class FirstLevelRetriesHandler
    {
        public FirstLevelRetriesHandler(FirstLevelRetryPolicy retryPolicy, BusNotifications notifications)
        {
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
        }

        public void LogRetryAttempt(IncomingMessage message, ProcessingFailureInfo failureInfo)
        {
            if (failureInfo?.NumberOfFailures > 0)
            {
                Logger.Info($"First Level Retry is going to retry message '{message.MessageId}' because of an exception:", failureInfo.Exception);
                
                //question: should we invoke this the first time around? feels like the naming is off?
                notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(failureInfo.NumberOfFailures - 1, message, failureInfo.Exception);
            }
        }

        public bool NumberOfRetiresNotExceeded(ProcessingFailureInfo failureInfo)
        {
            return failureInfo == null || retryPolicy.ShouldGiveUp(failureInfo.NumberOfFailures - 1) == false;
        }

        public void GiveUpForMessage(IncomingMessage message, ProcessingFailureInfo failureInfo)
        {
            message.Headers[Headers.FLRetries] = (failureInfo.NumberOfFailures - 1).ToString();

            //TODO: allthough the transaction will be rolled-back we want the header to be visible in notifications
            notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(failureInfo.NumberOfFailures, message, failureInfo.Exception);

            Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", message.MessageId);
        }
      
        FirstLevelRetryPolicy retryPolicy;
        BusNotifications notifications;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesHandler>();
    }
}