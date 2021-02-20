namespace NServiceBus.Transport
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Allows the transport to pass signal that a message has been completed.
    /// </summary>
    public class CompleteContext : IExtendable
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        /// <param name="messageId">Native message id.</param>
        /// <param name="wasAcknowledged">True if the message was acknowledged and removed from the queue.</param>
        /// <param name="context">A <see cref="ContextBag" /> which can be used to extend the current object.</param>
        public CompleteContext(string messageId, bool wasAcknowledged, ContextBag context)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);
            Guard.AgainstNull(nameof(context), context);

            MessageId = messageId;
            WasAcknowledged = wasAcknowledged;
            Extensions = context;
        }

        /// <summary>
        /// The native id of the message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// True if the message was acknowledged and removed from the queue.
        /// </summary>
        public bool WasAcknowledged { get; }

        /// <summary>
        /// A <see cref="ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; }
    }
}