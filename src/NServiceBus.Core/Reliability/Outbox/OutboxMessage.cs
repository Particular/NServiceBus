namespace NServiceBus.Outbox
{
    /// <summary>
    /// The Outbox message type.
    /// </summary>
    public class OutboxMessage
    {
        /// <summary>
        /// Creates an instance of an <see cref="OutboxMessage" />.
        /// </summary>
        /// <param name="messageId">The message identifier of the incoming message.</param>
        /// <param name="operations">The outgoing transport operations to execute as part of this incoming message.</param>
        public OutboxMessage(string messageId, TransportOperation[] operations)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);
            Guard.AgainstNull(nameof(operations), operations);

            MessageId = messageId;
            TransportOperations = operations;
        }

        /// <summary>
        /// Gets the message identifier of the incoming message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The list of operations performed during the processing of the incoming message.
        /// </summary>
        public TransportOperation[] TransportOperations { get; private set; }
    }
}