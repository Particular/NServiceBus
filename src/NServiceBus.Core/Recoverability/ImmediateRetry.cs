namespace NServiceBus
{
    using NServiceBus.Transport;

    /// <summary>
    /// Indicates recoverability is required to immediately retry the current message.
    /// </summary>
    public sealed class ImmediateRetry : RecoverabilityAction
    {
        internal ImmediateRetry() { }

        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.RetryRequired;
    }
}