namespace NServiceBus.SecondLevelRetries
{
    using System;

    abstract class SecondLevelRetryPolicy
    {
        public abstract bool TryGetDelay(TransportMessage message, Exception ex, int currentRetry,out TimeSpan delay);
    }
}