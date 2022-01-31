namespace NServiceBus
{
    using NServiceBus.Transport;

    /// <summary>
    /// Indicates recoverability is required to discard/ignore the current message.
    /// </summary>
    public sealed class Discard : RecoverabilityAction
    {
        internal Discard(string reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// The reason why a message was discarded.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// How to handle the message from a transport perspective.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;
    }
}