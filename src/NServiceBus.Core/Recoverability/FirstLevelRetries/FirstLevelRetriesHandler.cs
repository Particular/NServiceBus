namespace NServiceBus.Recoverability.FirstLevelRetries
{
    using System;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline.Contexts;

    internal class FirstLevelRetriesHandler
    {
        public FirstLevelRetriesHandler(FlrStatusStorage storage, FirstLevelRetryPolicy retryPolicy, BusNotifications notifications)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
        }
        internal bool TryHandle(string pipelineName, TransportReceiveContext context, Exception ex)
        {
            var messageId = context.Message.MessageId;
            var pipelineUniqueMessageId = pipelineName + messageId;

            var numberOfFailures = storage.GetFailuresForMessage(pipelineUniqueMessageId);

            if (retryPolicy.ShouldGiveUp(numberOfFailures))
            {
                storage.ClearFailuresForMessage(pipelineUniqueMessageId);
                context.Message.Headers[Headers.FLRetries] = numberOfFailures.ToString();

                //HINT: allthough the transaction will be rolled-back we want the header to be visible in notifications
                notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfFailures, context.Message, ex);
                Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);

                return false;
            }

            storage.IncrementFailuresForMessage(pipelineUniqueMessageId);

            Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", ex);
            //question: should we invoke this the first time around? feels like the naming is off?
            notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfFailures, context.Message, ex);

            return true;
        }

        FlrStatusStorage storage;
        FirstLevelRetryPolicy retryPolicy;
        BusNotifications notifications;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesHandler>();
    }
}