namespace NServiceBus
{
    /// <summary>
    /// Indicates recoverability is required to discard/ignore the current message.
    /// </summary>
    public sealed class Discard : RecoverabilityAction
    {
        internal Discard(string reason = null)
        {
            Reason = reason;
        }

        /// <summary>
        /// The reason why a message was discarded.
        /// </summary>
        public string Reason { get; }
    }
}