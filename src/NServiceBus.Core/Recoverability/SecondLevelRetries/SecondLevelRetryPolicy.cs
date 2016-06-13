namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    abstract class SecondLevelRetryPolicy
    {
        public abstract bool TryGetDelay(Dictionary<string,string> headers, Exception ex, int currentRetry, out TimeSpan delay);
    }
}