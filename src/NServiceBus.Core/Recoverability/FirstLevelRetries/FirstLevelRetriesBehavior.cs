namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
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

        public override void Invoke(Context context, Action next)
        {
            try
            {
                next();
            }
            catch (MessageDeserializationException)
            {
                throw; // no retries for poison messages
            }
            catch (Exception ex)
            {
                // TODO should we add piplineInfo.Name to the messageId?
                var messageId = context.GetPhysicalMessage().Id;

                var numberOfRetries = storage.GetRetriesForMessage(messageId);

                if (retryPolicy.ShouldGiveUp(numberOfRetries))
                {
                    storage.ClearFailuresForMessage(messageId);
                    context.GetPhysicalMessage().Headers[Headers.FLRetries] = numberOfRetries.ToString();
                    notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfRetries, context.GetPhysicalMessage(), ex);
                    throw;
                }

                storage.IncrementFailuresForMessage(messageId);

                //question: should we invoke this the first time around? feels like the naming is off?
                notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfRetries,context.GetPhysicalMessage(),ex);

                context.AbortReceiveOperation = true;
            }

        }

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