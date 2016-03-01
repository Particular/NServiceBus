namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;

    class CustomSecondLevelRetryPolicy : SecondLevelRetryPolicy
    {
        public CustomSecondLevelRetryPolicy(Func<ITransportReceiveContext, TimeSpan> customRetryPolicy)
        {
            this.customRetryPolicy = customRetryPolicy;
        }

        public override bool TryGetDelay(ITransportReceiveContext context, Exception ex, int currentRetry, out TimeSpan delay)
        {
            delay = customRetryPolicy(context);

            return delay != TimeSpan.Zero;
        }

        Func<ITransportReceiveContext, TimeSpan> customRetryPolicy;
    }
}