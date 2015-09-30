﻿namespace NServiceBus.Outbox
{
    using System.Collections.Generic;
    using System.Linq;

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
        public OutboxMessage(string messageId, IList<TransportOperation> operations)
        {
            Guard.AgainstNullAndEmpty("messageId", messageId);
            Guard.AgainstNull("operations", operations);

            MessageId = messageId;
            TransportOperations = operations.ToList();
        }

        /// <summary>
        ///     Gets the message identifier of the incoming message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        ///     The list of operations performed during the processing of the incoming message.
        /// </summary>
        public IList<TransportOperation> TransportOperations { get; private set; }
    }
}