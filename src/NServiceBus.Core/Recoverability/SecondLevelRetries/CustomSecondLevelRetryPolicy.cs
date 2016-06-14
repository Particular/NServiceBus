namespace NServiceBus
{
    using System;
    using Transports;

    class CustomSecondLevelRetryPolicy : SecondLevelRetryPolicy
    {
        public CustomSecondLevelRetryPolicy(Func<IncomingMessage, Exception, int, TimeSpan> customRetryPolicy)
        {
            this.customRetryPolicy = customRetryPolicy;
        }

        public override bool TryGetDelay(IncomingMessage message, Exception ex, int currentRetry, out TimeSpan delay)
        {
            delay = customRetryPolicy(message, ex, currentRetry);

            return delay != TimeSpan.MinValue;
        }

        Func<IncomingMessage, Exception, int, TimeSpan> customRetryPolicy;
    }
}