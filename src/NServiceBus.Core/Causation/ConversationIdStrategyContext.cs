namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;

    /// <summary>
    /// Provides context when generating message conversation IDs.
    /// </summary>
    public class ConversationIdStrategyContext
    {
        /// <summary>
        /// Creates a new context.
        /// </summary>
        public ConversationIdStrategyContext(OutgoingLogicalMessage message, IReadOnlyDictionary<string, string> headers)
        {
            Guard.AgainstNull(nameof(message), message);

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
        public IReadOnlyDictionary<string, string> Headers { get; }
    }
}