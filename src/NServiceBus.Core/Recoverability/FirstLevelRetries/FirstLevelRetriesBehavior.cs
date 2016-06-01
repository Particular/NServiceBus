namespace NServiceBus
{
    using System;
    using Logging;

    class FirstLevelRetriesBehavior
    {
        public FirstLevelRetriesBehavior(FirstLevelRetryPolicy retryPolicy)
        {
            this.retryPolicy = retryPolicy;
        }

        public bool Invoke(Exception exception, int firstLevelRetries, string messageId)
        {
            if (exception is MessageDeserializationException)
            {
                return false;
            }

            if (retryPolicy.ShouldGiveUp(firstLevelRetries))
            {
                Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);
                return false;
            }

            Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", exception);

            //await context.RaiseNotification(new MessageToBeRetried(firstLevelRetries, TimeSpan.Zero, context.Message, ex)).ConfigureAwait(false);

            return true;
        }

        FirstLevelRetryPolicy retryPolicy;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesBehavior>();
    }
}