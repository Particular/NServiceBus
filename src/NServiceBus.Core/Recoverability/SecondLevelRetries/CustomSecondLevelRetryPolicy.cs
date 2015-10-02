namespace NServiceBus.Recoverability.SecondLevelRetries
{
    using System;
    using NServiceBus.Transports;

    class CustomSecondLevelRetryPolicy : SecondLevelRetryPolicy
    {
        Func<IncomingMessage, TimeSpan> customRetryPolicy;

        public CustomSecondLevelRetryPolicy(Func<IncomingMessage, TimeSpan> customRetryPolicy)
        {
            this.customRetryPolicy = customRetryPolicy;
        }

        public override bool TryGetDelay(IncomingMessage message, Exception ex, int currentRetry, out TimeSpan delay)
        {
            delay = customRetryPolicy(message);

            return delay != TimeSpan.Zero;
        }
    }
}