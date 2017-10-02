namespace NServiceBus
{
    using Pipeline;

    /// <summary>
    /// Provides context when generating message conversation ID's.
    /// </summary>
    public class ConversationIdGeneratorContext
    {
        /// <summary>
        /// Creates a new context.
        /// </summary>
        public ConversationIdGeneratorContext(OutgoingLogicalMessage message)
        {
            Message = message;
        }

        /// <summary>
        /// The message to be sent.
        /// </summary>
        public OutgoingLogicalMessage Message { get; }
    }
}