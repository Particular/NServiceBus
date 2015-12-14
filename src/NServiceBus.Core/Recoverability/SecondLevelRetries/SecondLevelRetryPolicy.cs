namespace NServiceBus
{
    using System;
    using NServiceBus.Transports;

    abstract class SecondLevelRetryPolicy
    {
        public abstract bool TryGetDelay(IncomingMessage message, Exception ex, int currentRetry,out TimeSpan delay);
    }
}