namespace NServiceBus
{
    using System;
    using NServiceBus.Transport;

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

        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;
    }
}