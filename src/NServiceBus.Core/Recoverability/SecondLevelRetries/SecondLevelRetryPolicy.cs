namespace NServiceBus.Recoverability.SecondLevelRetries
{
    using System;
    using Transports;

    abstract class SecondLevelRetryPolicy
    {
        public abstract bool TryGetDelay(IncomingMessage message, Exception ex, int currentRetry, out TimeSpan delay);
    }
}