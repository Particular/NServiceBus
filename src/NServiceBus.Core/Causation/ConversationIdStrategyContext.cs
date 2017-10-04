namespace NServiceBus
{
    using Pipeline;

    /// <summary>
    /// Provides context when generating message conversation ID's.
    /// </summary>
    public class ConversationIdStrategyContext
    {
        /// <summary>
        /// Creates a new context.
        /// </summary>
        public ConversationIdStrategyContext(OutgoingLogicalMessage message)
        {
            Guard.AgainstNull(nameof(message), message);

            Message = message;
        }

        /// <summary>
        /// The message to be sent.
        /// </summary>
        public OutgoingLogicalMessage Message { get; }
    }
}