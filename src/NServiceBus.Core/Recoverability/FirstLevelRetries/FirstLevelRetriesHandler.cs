namespace NServiceBus.Recoverability.FirstLevelRetries
{
    using System;
    using NServiceBus.Logging;
    using NServiceBus.Transports;

    internal class FirstLevelRetriesHandler
    {
        public FirstLevelRetriesHandler(FlrStatusStorage storage, FirstLevelRetryPolicy retryPolicy, BusNotifications notifications)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
        }

        internal void MarkFailure(string uniqueMessageId, Exception exception)
        {
            storage.AddFailuresForMessage(uniqueMessageId, exception);
        }

        internal bool TryHandle(string uniqueMessageId, IncomingMessage message, out ProcessingFailureInfo failureInfo)
        {
            failureInfo = storage.GetFailuresForMessage(uniqueMessageId);

            if (failureInfo == null)
            {
                return true;
            }

            var messageId = message.MessageId;

            //TODO: this is to obscure. Retry policy expects retries that happened and not failures
            if (retryPolicy.ShouldGiveUp(failureInfo.NumberOfFailures-1))
            {
                storage.ClearFailuresForMessage(uniqueMessageId);
                message.Headers[Headers.FLRetries] = (failureInfo.NumberOfFailures-1).ToString();

                //TODO: allthough the transaction will be rolled-back we want the header to be visible in notifications
                notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(failureInfo.NumberOfFailures, message, failureInfo.Exception);
                Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);

                return false;
            }

            if (failureInfo.NumberOfFailures > 0)
            {
                Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", failureInfo.Exception);
                //question: should we invoke this the first time around? feels like the naming is off?
                notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(failureInfo.NumberOfFailures-1, message, failureInfo.Exception);
            }

            return true;
        }

        FlrStatusStorage storage;
        FirstLevelRetryPolicy retryPolicy;
        BusNotifications notifications;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesHandler>();
    }
}