namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Transports;

    class RecoverabilityPolicy
    {
        FirstLevelRetriesBehavior firstLevelRetries;
        SecondLevelRetriesBehavior secondLevelRetries;
        MoveFaultsToErrorQueueBehavior errorQueue;

        public RecoverabilityPolicy()
        {
            firstLevelRetries = new FirstLevelRetriesBehavior(new FirstLevelRetryPolicy(1));
            secondLevelRetries = new SecondLevelRetriesBehavior(
                new DefaultSecondLevelRetryPolicy(1, TimeSpan.FromSeconds(2)),
                string.Empty,
                new FailureInfoStorage(1000),
                null);

            errorQueue = new MoveFaultsToErrorQueueBehavior(new CriticalError(c => Task.FromResult(0)));
        }

        public async Task<bool> Invoke(ErrorContext context, IDispatchMessages messageDispatcher, string errorQueueAddress)
        {
            var numberOfSecondLevelRetries = GetSecondLevelRetryAttemptFromHeaders();
            var firstLevelAttempt = context.NumberOfProcessingAttempts/numberOfSecondLevelRetries;

            var retryImmediately = firstLevelRetries.Invoke(context.Exception, firstLevelAttempt, context.MessageId);
            if (retryImmediately)
            {
                return true;
            }

            var incomingMesage = new IncomingMessage(context.MessageId, context.Headers, context.BodyStream);
            var deferedRetry = await secondLevelRetries.Invoke(context.Exception, numberOfSecondLevelRetries, incomingMesage, context.Context).ConfigureAwait(false);
            if (deferedRetry)
            {
                return false;
            }

            await errorQueue.Invoke(errorQueueAddress, incomingMesage, context.Exception, messageDispatcher, context.Context).ConfigureAwait(false);

            return false;
        }

        int GetSecondLevelRetryAttemptFromHeaders()
        {
            throw new NotImplementedException();
        }
    }
}