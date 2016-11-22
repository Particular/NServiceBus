namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The message going out to the transport.
    /// </summary>
    public class OutgoingMessage
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OutgoingMessage" />.
        /// </summary>
        /// <param name="messageId">The message id to use.</param>
        /// <param name="headers">The headers associated with this message.</param>
        /// <param name="body">The body of the message.</param>
        public OutgoingMessage(string messageId, Dictionary<string, string> headers, byte[] body)
        {
            MessageId = messageId;
            Headers = headers;
            Body = body;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OutgoingMessage" />.
        /// </summary>
        /// <param name="messageId">The message id to use.</param>
        /// <param name="headers">The headers associated with this message.</param>
        /// <param name="body">The body of the message.</param>
        public OutgoingMessage(string messageId, Dictionary<string, string> headers, ArraySegment<byte> body)
        {
            MessageId = messageId;
            Headers = headers;
            BodySegment = body;
        }

        /// <summary>
        /// The body to be sent.
        /// </summary>
        public byte[] Body { get; }

        /// <summary>
        /// The body to be sent.
        /// </summary>
        public ArraySegment<byte> BodySegment { get; }

        /// <summary>
        /// The id of the message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// The headers for the message.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        internal OutgoingMessage Clone()
        {
            return Body == null ? new OutgoingMessage(MessageId, Headers, BodySegment) : new OutgoingMessage(MessageId, Headers, Body);
        }
    }
}