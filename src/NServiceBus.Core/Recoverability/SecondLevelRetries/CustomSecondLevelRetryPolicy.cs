namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    class CustomSecondLevelRetryPolicy : SecondLevelRetryPolicy
    {
        public CustomSecondLevelRetryPolicy(Func<Dictionary<string, string>, TimeSpan> customRetryPolicy)
        {
            this.customRetryPolicy = customRetryPolicy;
        }

        public override bool TryGetDelay(Dictionary<string, string> headers, Exception ex, int currentRetry, out TimeSpan delay)
        {
            delay = customRetryPolicy(headers);

            return delay != TimeSpan.MinValue;
        }

        Func<Dictionary<string, string>, TimeSpan> customRetryPolicy;
    }
}