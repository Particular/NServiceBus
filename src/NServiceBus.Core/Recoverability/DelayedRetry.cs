namespace NServiceBus
{
    using System;

    /// <summary>
    /// Indicates recoverability is required to delay retry the current message.
    /// </summary>
    public sealed class DelayedRetry : RecoverabilityAction
    {
        internal DelayedRetry(TimeSpan delay)
        {
            Delay = delay;
        }

        /// <summary>
        /// The retry delay.
        /// </summary>
        public TimeSpan Delay { get; }
    }
}