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
        /// <param name="operations">The outgoing transport operations to execute as part of this incoming message.</param>
        public OutboxMessage(string messageId,IEnumerable<TransportOperation> operations = null)
        {
            Guard.AgainstNullAndEmpty("messageId", messageId);
         
            MessageId = messageId;

            if (operations != null)
            {
                TransportOperations = operations;
            }
            else
            {
                TransportOperations = new List<TransportOperation>();
            }
        }

        /// <summary>
        ///     Gets the message identifier of the incoming message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        ///     The list of operations performed during the processing of the incoming message.
        /// </summary>
        public IEnumerable<TransportOperation> TransportOperations { get; private set; }
    }
}