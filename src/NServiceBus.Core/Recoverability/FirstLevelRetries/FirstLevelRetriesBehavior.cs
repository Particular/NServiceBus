namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using Pipeline;
    using Pipeline.Contexts;

    class FirstLevelRetriesBehavior : Behavior<ITransportReceiveContext>
    {
        public FirstLevelRetriesBehavior(
            FlrStatusStorage storage, 
            FirstLevelRetryPolicy retryPolicy, 
            BusNotifications notifications,
            string uniqueKey)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
            this.uniqueKey = uniqueKey;
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
                var pipelineUniqueMessageId = uniqueKey + messageId;

                var numberOfFailures = storage.GetFailuresForMessage(pipelineUniqueMessageId);

                if (retryPolicy.ShouldGiveUp(numberOfFailures))
                {
                    storage.ClearFailuresForMessage(pipelineUniqueMessageId);
                    context.Message.Headers[Headers.FLRetries] = numberOfFailures.ToString();
                    notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfFailures, context.Message, ex);
                    Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);
                    throw;
                }

                storage.IncrementFailuresForMessage(pipelineUniqueMessageId);

                Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", ex);
                //question: should we invoke this the first time around? feels like the naming is off?
                notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfFailures, context.Message, ex);

                context.AbortReceiveOperation();
            }
        }

        FlrStatusStorage storage;
        FirstLevelRetryPolicy retryPolicy;
        BusNotifications notifications;
        string uniqueKey;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesBehavior>();

        public class Registration : RegisterStep
        {
            public Registration(string uniqueKey) 
                : base("FirstLevelRetries", typeof(FirstLevelRetriesBehavior), "Performs first level retries", 
                      b => new FirstLevelRetriesBehavior(b.Build<FlrStatusStorage>(), b.Build<FirstLevelRetryPolicy>(), b.Build<BusNotifications>(), uniqueKey))
            {
            }

            public override bool IsEnabled(ReadOnlySettings settings)
            {
                return settings.IsFeatureActive(typeof(FirstLevelRetries));
            }
        }
    }
}