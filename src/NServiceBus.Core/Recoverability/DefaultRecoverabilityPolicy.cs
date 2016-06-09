namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    class DefaultRecoverabilityPolicy : IRecoverabilityPolicy
    {
        public DefaultRecoverabilityPolicy(SecondLevelRetryPolicy secondLevelRetryPolicy, int maxImmediateRetries)
        {
            this.secondLevelRetryPolicy = secondLevelRetryPolicy;
            this.maxImmediateRetries = maxImmediateRetries;
        }


        public RecoveryAction Invoke(Exception exception, Dictionary<string, string> headers, int numberOfProcessingAttempts, Dictionary<string, string> metadata)
        {
            if (exception is MessageDeserializationException)
            {
                return new MoveToErrorQueue();
            }
            var numberOfDelayedRetryAttempts = metadata.ContainsKey(Headers.Retries)
                ? int.Parse(metadata[Headers.Retries])
                : 0;

            var numberOfImmediateRetries = numberOfDelayedRetryAttempts == 0
                ? numberOfProcessingAttempts
                : numberOfProcessingAttempts / numberOfDelayedRetryAttempts;

            if (ShouldDoImmediateRetry(numberOfImmediateRetries))
            {
                return new ImmediateRetry();
            }

            TimeSpan delay;

            if (secondLevelRetryPolicy != null && 
                secondLevelRetryPolicy.TryGetDelay(headers, exception, numberOfDelayedRetryAttempts, out delay))
            {
                return new DelayedRetry(delay, new Dictionary<string, string>
                {
                    {Headers.Retries, (numberOfDelayedRetryAttempts + 1).ToString()}
                });
            }
            
            return new MoveToErrorQueue();
        }

        bool ShouldDoImmediateRetry(int numberOfImmediateRetries)
        {
            return numberOfImmediateRetries <= maxImmediateRetries;
        }

        SecondLevelRetryPolicy secondLevelRetryPolicy;
        int maxImmediateRetries;
    }
}