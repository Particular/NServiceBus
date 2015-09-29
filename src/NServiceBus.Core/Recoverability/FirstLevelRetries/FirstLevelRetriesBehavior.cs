namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using NServiceBus.Settings;

    class FirstLevelRetriesBehavior : PhysicalMessageProcessingStageBehavior
    {
        FlrStatusStorage storage;
        FirstLevelRetryPolicy retryPolicy;
        BusNotifications notifications;

        public FirstLevelRetriesBehavior(FlrStatusStorage storage, FirstLevelRetryPolicy retryPolicy, BusNotifications notifications)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
        }

        public override async Task Invoke(Context context, Func<Task> next)
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
                var messageId = context.GetPhysicalMessage().Id;
                var pipelineUniqueMessageId = PipelineInfo.Name + messageId;

                var numberOfFailures = storage.GetFailuresForMessage(pipelineUniqueMessageId);

                if (retryPolicy.ShouldGiveUp(numberOfFailures))
                {
                    storage.ClearFailuresForMessage(pipelineUniqueMessageId);
                    context.GetPhysicalMessage().Headers[Headers.FLRetries] = numberOfFailures.ToString();
                    notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfFailures, context.GetPhysicalMessage(), ex);
                    Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);
                    throw;
                }

                storage.IncrementFailuresForMessage(pipelineUniqueMessageId);

                Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", ex);
                //question: should we invoke this the first time around? feels like the naming is off?
                notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfFailures,context.GetPhysicalMessage(),ex);

                context.AbortReceiveOperation = true;
            }

            
        }

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("FirstLevelRetries", typeof(FirstLevelRetriesBehavior), "Performs first level retries")
            {
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }

            public override bool IsEnabled(ReadOnlySettings settings)
            {
                return settings.IsFeatureActive(typeof(FirstLevelRetries));
            }
        }
    }
}