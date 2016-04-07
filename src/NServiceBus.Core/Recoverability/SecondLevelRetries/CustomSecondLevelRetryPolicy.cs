namespace NServiceBus
{
    using System;
    using Transports;

    class CustomSecondLevelRetryPolicy : SecondLevelRetryPolicy
    {
        public CustomSecondLevelRetryPolicy(Func<IncomingMessage, TimeSpan> customRetryPolicy)
        {
            this.customRetryPolicy = customRetryPolicy;
        }

        public override bool TryGetDelay(IncomingMessage message, Exception ex, int currentRetry, out TimeSpan delay)
        {
            delay = customRetryPolicy(message);

            return delay != TimeSpan.MinValue;
        }

        Func<IncomingMessage, TimeSpan> customRetryPolicy;
    }
}