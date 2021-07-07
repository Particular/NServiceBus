namespace NServiceBus
{
    using Pipeline;

    /// <summary>
    /// Provides context when generating message conversation IDs.
    /// </summary>
    public class ConversationIdStrategyContext
    {
        /// <summary>
        /// Creates a new context.
        /// </summary>
        public ConversationIdStrategyContext(OutgoingLogicalMessage message, HeaderDictionary headers)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(headers), headers);

            Message = message;
            Headers = headers;
        }

        /// <summary>
        /// The message to be sent.
        /// </summary>
        public OutgoingLogicalMessage Message { get; }


        /// <summary>
        /// The headers attached to the outgoing message.
        /// </summary>
        public HeaderDictionary Headers { get; }
    }
}