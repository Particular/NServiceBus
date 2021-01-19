namespace NServiceBus.Outbox
{
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    /// Outgoing message operation.
    /// </summary>
    public class TransportOperation
    {
        /// <summary>
        /// Creates a new instance of a <see cref="TransportOperation" />.
        /// </summary>
        public TransportOperation(string messageId, DispatchProperties properties, byte[] body, Dictionary<string, string> headers)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);

            MessageId = messageId;
            Properties = properties;
            Body = body;
            Headers = headers;
        }

        /// <summary>
        /// Gets the identifier of the outgoing message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// Transport specific dispatch operation properties.
        /// </summary>
        public DispatchProperties Properties { get; }

        /// <summary>
        /// Gets a byte array to the body content of the outgoing message.
        /// </summary>
        public byte[] Body { get; }

        /// <summary>
        /// Gets outgoing message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }
    }
}