namespace NServiceBus
{
    using System;

    sealed class DelayedRetry : RecoverabilityAction
    {
        internal DelayedRetry(TimeSpan delay)
        {
            Delay = delay;
        }

        public TimeSpan Delay { get; private set; }
    }
}