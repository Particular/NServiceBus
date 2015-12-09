namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using Pipeline;
    using Pipeline.Contexts;
    using Recoverability.FirstLevelRetries;

    class FirstLevelRetriesBehavior : Behavior<TransportReceiveContext>
    {
        public FirstLevelRetriesBehavior(FlrStatusStorage storage, FirstLevelRetryPolicy retryPolicy, IEnumerable<Action<FirstLevelRetry>> firstLevelRetryActions)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
            this.firstLevelRetryActions = firstLevelRetryActions.ToList();
        }

        public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
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
                var pipelineUniqueMessageId = PipelineInfo.Name + messageId;

                var numberOfFailures = storage.GetFailuresForMessage(pipelineUniqueMessageId);

                if (retryPolicy.ShouldGiveUp(numberOfFailures))
                {
                    storage.ClearFailuresForMessage(pipelineUniqueMessageId);
                    context.Message.Headers[Headers.FLRetries] = numberOfFailures.ToString();
                    InvokeNotification(numberOfFailures, exception, context.Message);
                    Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);
                    throw;
                }

                storage.IncrementFailuresForMessage(pipelineUniqueMessageId);

                Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", exception);
                //question: should we invoke this the first time around? feels like the naming is off?
                InvokeNotification(numberOfFailures, exception, context.Message);

                throw new MessageProcessingAbortedException();
            }
        }

        void InvokeNotification(int numberOfFailures, Exception exception, IncomingMessage message)
        {
            var secondLevelRetry = new FirstLevelRetry(message.MessageId, message.Headers, message.Body, exception, numberOfFailures);
            foreach (var firstLevelRetryAction in firstLevelRetryActions)
            {
                firstLevelRetryAction(secondLevelRetry);
            }
        }

        FlrStatusStorage storage;
        FirstLevelRetryPolicy retryPolicy;
        List<Action<FirstLevelRetry>> firstLevelRetryActions;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesBehavior>();

        public class Registration : RegisterStep
        {
            public Registration() : base("FirstLevelRetries", typeof(FirstLevelRetriesBehavior), "Performs first level retries")
            {
            }

            public override bool IsEnabled(ReadOnlySettings settings)
            {
                return settings.IsFeatureActive(typeof(FirstLevelRetries));
            }
        }
    }
}