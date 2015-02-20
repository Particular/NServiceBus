namespace NServiceBus.SecondLevelRetries
{
    using System;

    class CustomSecondLevelRetryPolicy : SecondLevelRetryPolicy
    {
        readonly Func<TransportMessage, TimeSpan> customRetryPolicy;

        public CustomSecondLevelRetryPolicy(Func<TransportMessage, TimeSpan> customRetryPolicy)
        {
            this.customRetryPolicy = customRetryPolicy;
        }

        public override bool TryGetDelay(TransportMessage message, Exception ex, int currentRetry, out TimeSpan delay)
        {
            delay = customRetryPolicy(message);

            return delay != TimeSpan.Zero;
        }
    }
}