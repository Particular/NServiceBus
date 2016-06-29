namespace NServiceBus
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class FirstLevelRetriesHandler
    {
        public FirstLevelRetriesHandler(FailureInfoStorage storage, FirstLevelRetryPolicy retryPolicy)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
        }

        public async Task<bool> HandleMessageFailure(ITransportReceiveContext context, Exception ex)
        {
            var messageId = context.Message.MessageId;
            var pipelineUniqueMessageId = messageId;

            var firstLevelRetries = storage.GetFailureInfoForMessage(pipelineUniqueMessageId).FLRetries;

            if (retryPolicy.ShouldGiveUp(firstLevelRetries))
            {
                Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);
                return false;
            }

            Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", ex);

            await context.RaiseNotification(new MessageToBeRetried(firstLevelRetries, TimeSpan.Zero, context.Message, ex)).ConfigureAwait(false);

            storage.RecordFirstLevelRetryAttempt(pipelineUniqueMessageId, ExceptionDispatchInfo.Capture(ex));

            context.AbortReceiveOperation();

            return true;
        }

        FirstLevelRetryPolicy retryPolicy;
        FailureInfoStorage storage;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesHandler>();
    }
}