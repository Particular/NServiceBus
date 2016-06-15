namespace NServiceBus
{
    using System;

    class CustomSecondLevelRetryPolicy : SecondLevelRetryPolicy
    {
        public CustomSecondLevelRetryPolicy(Func<SecondLevelRetryContext, TimeSpan> customRetryPolicy)
        {
            this.customRetryPolicy = customRetryPolicy;
        }

        Func<SecondLevelRetryContext, TimeSpan> customRetryPolicy;
        public override bool TryGetDelay(SecondLevelRetryContext slrRetryContext, out TimeSpan delay)
        {
            delay = customRetryPolicy(slrRetryContext);

            return delay != TimeSpan.MinValue;
        }
    }
}