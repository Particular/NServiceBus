namespace NServiceBus.Outbox
{
    using System.Collections.Generic;

    /// <summary>
    ///     The Outbox message type.
    /// </summary>
    public class OutboxMessage
    {
        /// <summary>
        ///     Creates an instance of an <see cref="OutboxMessage" />.
        /// </summary>
        /// <param name="messageId">The message identifier of the incoming message.</param>
        public OutboxMessage(string messageId)
        {
            Guard.AgainstNullAndEmpty(messageId, "messageId");

            MessageId = messageId;
        }

        /// <summary>
        ///     Gets the message identifier of the incoming message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        ///     The list of operations performed during the processing of the incoming message.
        /// </summary>
        public List<TransportOperation> TransportOperations
        {
            get
            {
                if (transportOperations == null)
                {
                    transportOperations = new List<TransportOperation>();
                }

                return transportOperations;
            }
        }

        List<TransportOperation> transportOperations;
    }
}