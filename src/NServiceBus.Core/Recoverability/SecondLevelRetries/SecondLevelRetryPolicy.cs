namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;

    abstract class SecondLevelRetryPolicy
    {
        public abstract bool TryGetDelay(ITransportReceiveContext context, Exception ex, int currentRetry,out TimeSpan delay);
    }
}