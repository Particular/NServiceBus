namespace NServiceBus
{
    using NServiceBus.Transport;

    /// <summary>
    /// Indicates that recoverability is required to move the current message to the error queue.
    /// </summary>
    public sealed class MoveToError : RecoverabilityAction
    {
        internal MoveToError(string errorQueue)
        {
            ErrorQueue = errorQueue;
        }

        /// <summary>
        /// Defines the error queue where the message should be move to.
        /// </summary>
        public string ErrorQueue { get; }

        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;
    }
}