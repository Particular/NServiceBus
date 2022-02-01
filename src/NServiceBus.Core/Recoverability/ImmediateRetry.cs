namespace NServiceBus
{
    using System.Collections.Generic;
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

        /// <summary>
        /// Executes the recoverability action.
        /// </summary>
        public override IEnumerable<TransportOperation> Execute(
            ErrorContext errorContext,
            IDictionary<string, string> metadata)
        {
            yield break;
        }
    }
}