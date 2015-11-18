namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Recoverability.Faults;
    using NServiceBus.Recoverability.FirstLevelRetries;

    class RecoverabilityBehavior : Behavior<TransportReceiveContext>
    {
        public RecoverabilityBehavior(
            FlrStatusStorage failureStorage,
            MoveFaultsToErrorQueueHandler faults,
            FirstLevelRetriesHandler flr,
            SecondLevelRetriesHandler slr)
        {
            this.failureStorage = failureStorage;
            this.faults = faults;
            this.flr = flr;
            this.slr = slr;
        }

        public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
        {
            //TODO: this is bad :/ - we probably need to have a spearate class for timeout manager and separate suit of tests
            slr.Suppressed &= PipelineInfo.Name == "Timeout Message Processor" || PipelineInfo.Name == "Timeout Dispatcher Processor";

            var uniqueMessageId = PipelineInfo.Name + context.Message.MessageId;

            var failureInfo = failureStorage.GetFailuresForMessage(uniqueMessageId);

            //TODO: we need to extract the failure at the begining as it might be clearn from LRU cache
            if (flr.NumberOfRetiresNotExceeded(failureInfo))
            {
                flr.LogRetryAttempt(context.Message, failureInfo);

                try
                {
                    await next().ConfigureAwait(false);
                    return;
                }
                catch (MessageDeserializationException)
                {
                    //we do not handle poison messages
                    throw;
                }
                catch (Exception ex)
                {
                    failureStorage.AddFailuresForMessage(uniqueMessageId, ex);

                    throw new MessageProcessingAbortedException();
                }
            }

            flr.GiveUpForMessage(context.Message, failureInfo);

            if (slr.NumberOfRetriesNotExceeded(context.Message, failureInfo.Exception))
            {
                slr.LogRetryAttempt(context.Message, failureInfo.Exception);

                try
                {
                    await slr.MoveToTimeoutQueue(context, failureInfo.Exception);
                    return;
                }
                catch (Exception ex)
                {
                    await faults.MoveToErrorQueue(context, ex, PipelineInfo.TransportAddress);
                    return;
                }
            }

            slr.GiveUpForMessage(context.Message);

            await faults.MoveToErrorQueue(context, failureInfo.Exception, PipelineInfo.TransportAddress);

            failureStorage.ClearFailuresForMessage(uniqueMessageId);
        }

        readonly FlrStatusStorage failureStorage;
        readonly MoveFaultsToErrorQueueHandler faults;
        readonly FirstLevelRetriesHandler flr;
        readonly SecondLevelRetriesHandler slr;

        public class Registration : RegisterStep
        {
            public Registration()
                : base("Recoverability", typeof(RecoverabilityBehavior), "Moved failing messages to the configured error queue")
            {
                //TODO: this should probably be first behavior in the pipeline
            }
        }
    }
}