namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class FirstLevelRetriesBehavior : Behavior<ITransportReceiveContext>
    {
        public FirstLevelRetriesBehavior(FlrStatusStorage storage, FirstLevelRetryPolicy retryPolicy)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageDeserializationException)
            {
                throw; // no retries for poison messages
            }
            catch (Exception ex)
            {
                var messageId = context.Message.MessageId;
                var pipelineUniqueMessageId = messageId;

                var numberOfFailures = storage.GetFailuresForMessage(pipelineUniqueMessageId);

                if (retryPolicy.ShouldGiveUp(numberOfFailures))
                {
                    storage.ClearFailuresForMessage(pipelineUniqueMessageId);
                    context.Message.Headers[Headers.FLRetries] = numberOfFailures.ToString();
                    Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);
                    throw;
                }

                storage.IncrementFailuresForMessage(pipelineUniqueMessageId);

                Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", ex);

                await context.RaiseNotification(new MessageToBeRetried(numberOfFailures, TimeSpan.Zero, context.Message, ex)).ConfigureAwait(false);

                context.AbortReceiveOperation();
            }
        }

        FirstLevelRetryPolicy retryPolicy;
        FlrStatusStorage storage;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesBehavior>();
    }
}