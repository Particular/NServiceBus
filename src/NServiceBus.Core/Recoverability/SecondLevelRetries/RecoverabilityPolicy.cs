namespace NServiceBus
{
    using System.Threading.Tasks;
    using Transports;

    class RecoverabilityPolicy
    {
        FirstLevelRetriesBehavior firstLevelRetries;
        SecondLevelRetriesBehavior secondLevelRetries;
        MoveFaultsToErrorQueueBehavior errorQueue;

        public Task<bool> Invoke(ErrorContext context)
        {
            var numberOfSecondLevelRetries = GetSecondLevelRetryAttemptFromHeaders();
            var firstLevelAttempt = context.NumberOfProcessingAttempts/numberOfSecondLevelRetries;

            var retryImmediately = firstLevelRetries.Invoke(context.Exception, firstLevelAttempt, context.MessageId);
            if (retryImmediately)
            {
                return Task.FromResult(true);
            }


        }

        int GetSecondLevelRetryAttemptFromHeaders()
        {
            throw new System.NotImplementedException();
        }
    }
}