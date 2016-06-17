namespace NServiceBus
{
    using System;

    abstract class SecondLevelRetryPolicy
    {
        public abstract bool TryGetDelay(SecondLevelRetryContext slrRetryContext, out TimeSpan delay);
    }
}