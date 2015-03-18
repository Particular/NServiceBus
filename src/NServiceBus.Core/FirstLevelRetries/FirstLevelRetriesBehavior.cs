namespace NServiceBus
{
    using System;
    using NServiceBus.FirstLevelRetries;
    using NServiceBus.Pipeline;

    class FirstLevelRetriesBehavior : PhysicalMessageProcessingStageBehavior
    {
        readonly FlrStatusStorage storage;
        readonly FirstLevelRetryPolicy retryPolicy;
        readonly BusNotifications notifications;

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
                var messageId = context.PhysicalMessage.Id;

                var numberOfRetries = storage.GetRetriesForMessage(messageId);

                if (retryPolicy.ShouldGiveUp(numberOfRetries))
                {
                    storage.ClearFailuresForMessage(messageId);
                    context.PhysicalMessage.Headers[Headers.FLRetries] = numberOfRetries.ToString();
                    notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfRetries, context.PhysicalMessage, ex);
                    throw;
                }

                storage.IncrementFailuresForMessage(messageId, ex);

                //question: should we invoke this the first time around? feels like the naming is off?
                notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfRetries,context.PhysicalMessage,ex);

                context.AbortReceiveOperation();
            }

        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("FirstLevelRetries", typeof(FirstLevelRetriesBehavior), "Performs first level retries", true)
            {
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }

    }
}