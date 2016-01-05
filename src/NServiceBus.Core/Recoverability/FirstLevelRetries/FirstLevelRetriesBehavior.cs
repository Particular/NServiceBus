namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using Pipeline;
    using Pipeline.Contexts;

    class FirstLevelRetriesBehavior : Behavior<ITransportReceiveContext>
    {
        public FirstLevelRetriesBehavior(
            FlrStatusStorage storage,
            FirstLevelRetryPolicy retryPolicy,
            Func<FirstLevelRetry, Task> notification,
            string uniqueKey)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
            this.notification = notification;
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
            catch (Exception exception)
            {
                var messageId = context.Message.MessageId;
                var pipelineUniqueMessageId = uniqueKey + messageId;

                var numberOfFailures = storage.GetFailuresForMessage(pipelineUniqueMessageId);

                if (retryPolicy.ShouldGiveUp(numberOfFailures))
                {
                    storage.ClearFailuresForMessage(pipelineUniqueMessageId);
                    context.Message.Headers[Headers.FLRetries] = numberOfFailures.ToString();
                    await InvokeNotification(numberOfFailures, exception, context.Message);
                    Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);
                    throw;
                }

                storage.IncrementFailuresForMessage(pipelineUniqueMessageId);

                Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", exception);
                //question: should we invoke this the first time around? feels like the naming is off?
                await InvokeNotification(numberOfFailures, exception, context.Message);

                throw new MessageProcessingAbortedException();
            }
        }

        Task InvokeNotification(int numberOfFailures, Exception exception, IncomingMessage message)
        {
            if (notification == null)
            {
                return TaskEx.Completed;
            }
            var secondLevelRetry = new FirstLevelRetry(message.MessageId, message.Headers, message.Body, exception, numberOfFailures);
            return notification(secondLevelRetry);
        }

        FlrStatusStorage storage;
        FirstLevelRetryPolicy retryPolicy;
        Func<FirstLevelRetry, Task> notification;
        string uniqueKey;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesBehavior>();

        public class Registration : RegisterStep
        {
            public Registration(string uniqueKey, ReadOnlySettings settings)
                : base("FirstLevelRetries", typeof(FirstLevelRetriesBehavior), "Performs first level retries",
                    b => new FirstLevelRetriesBehavior(b.Build<FlrStatusStorage>(), b.Build<FirstLevelRetryPolicy>(), settings.GetFirstLevelRetryNotification(), uniqueKey))
            {
            }

            public override bool IsEnabled(ReadOnlySettings settings)
            {
                return settings.IsFeatureActive(typeof(FirstLevelRetries));
            }
        }
    }
}