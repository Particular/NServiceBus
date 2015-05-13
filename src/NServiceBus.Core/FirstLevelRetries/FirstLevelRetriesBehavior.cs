namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using System.Threading.Tasks;
    using NServiceBus.FirstLevelRetries;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class FirstLevelRetriesBehavior : PhysicalMessageProcessingStageBehavior
    {
        FlrStatusStorage storage;
        FirstLevelRetryPolicy retryPolicy;
        BusNotifications notifications;

        public FirstLevelRetriesBehavior(FirstLevelRetryPolicy retryPolicy, BusNotifications notifications)
            : this(new FlrStatusStorage(), retryPolicy, notifications)
        {
        }

        public static FirstLevelRetriesBehavior CreateForTests(FlrStatusStorage storage, FirstLevelRetryPolicy retryPolicy, BusNotifications notifications)
        {
            return new FirstLevelRetriesBehavior(storage, retryPolicy, notifications);
        }

        FirstLevelRetriesBehavior(FlrStatusStorage storage, FirstLevelRetryPolicy retryPolicy, BusNotifications notifications)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            try
            {
                await next();
            }
            catch (MessageDeserializationException)
            {
                throw; // no retries for poison messages
            }
            catch (Exception ex)
            {
                var messageId = context.GetPhysicalMessage().Id;

                var numberOfRetries = storage.GetRetriesForMessage(messageId);

                if (retryPolicy.ShouldGiveUp(numberOfRetries))
                {
                    storage.ClearFailuresForMessage(messageId);
                    context.GetPhysicalMessage().Headers[Headers.FLRetries] = numberOfRetries.ToString();
                    notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfRetries, context.GetPhysicalMessage(), ex);
                    throw;
                }

                storage.IncrementFailuresForMessage(messageId, ex);

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
                return settings.IsFeatureActive(typeof(Features.FirstLevelRetries));
            }
        }

    }
}