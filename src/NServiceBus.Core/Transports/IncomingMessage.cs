namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The raw message coming from the transport.
    /// </summary>
    public class IncomingMessage
    {
        /// <summary>
        /// Creates a new message.
        /// </summary>
        /// <param name="messageId">Native message id.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="body">The message body.</param>
        public IncomingMessage(string messageId, Dictionary<string, string> headers, byte[] body)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);
            Guard.AgainstNull(nameof(body), body);
            Guard.AgainstNull(nameof(headers), headers);

            string originalMessageId;

            if (headers.TryGetValue(NServiceBus.Headers.MessageId, out originalMessageId) && !string.IsNullOrEmpty(originalMessageId))
            {
                MessageId = originalMessageId;
            }
            else
            {
                MessageId = messageId;

                headers[NServiceBus.Headers.MessageId] = messageId;
            }


            Headers = headers;

            Body = body;
        }

        /// <summary>
        /// Creates a new message.
        /// </summary>
        /// <param name="messageId">Native message id.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="segment">The message body.</param>
        public IncomingMessage(string messageId, Dictionary<string, string> headers, ArraySegment<byte> segment)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);
            Guard.AgainstNull(nameof(segment), segment);
            Guard.AgainstNull(nameof(headers), headers);

            string originalMessageId;

            if (headers.TryGetValue(NServiceBus.Headers.MessageId, out originalMessageId) && !string.IsNullOrEmpty(originalMessageId))
            {
                MessageId = originalMessageId;
            }
            else
            {
                MessageId = messageId;

                headers[NServiceBus.Headers.MessageId] = messageId;
            }


            Headers = headers;

            BodySegment = segment;
        }

        /// <summary>
        /// The id of the message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Gets/sets a byte array to the body content of the message.
        /// </summary>
        public byte[] Body { get; private set; }

        /// <summary>
        /// Gets/sets a byte array segment to the body content of the message.
        /// </summary>
        public ArraySegment<byte> BodySegment { get; private set; }

        /// <summary>
        /// Use this method to update the body if this message.
        /// </summary>
        internal void UpdateBody(byte[] updatedBody)
        {
            //preserve the original body if needed
            if (originalBody == null)
            {
                if (Body != null)
                {
                    originalBody = new byte[Body.Length];
                    Buffer.BlockCopy(Body, 0, originalBody, 0, Body.Length);
                }

                if (BodySegment.Array != null)
                {
                    originalBody = new byte[BodySegment.Count];
                    Buffer.BlockCopy(BodySegment.Array, BodySegment.Offset, originalBody, 0, BodySegment.Count);
                }
            }

            Body = updatedBody;
            BodySegment = default(ArraySegment<byte>);
        }

        /// <summary>
        /// Makes sure that the body is reset to the exact state as it was when the message was created.
        /// </summary>
        internal void RevertToOriginalBodyIfNeeded()
        {
            if (originalBody != null)
            {
                Body = originalBody;
            }
        }

        byte[] originalBody;

        internal OutgoingMessage ToOutgoingMessage()
        {
            return Body == null ? new OutgoingMessage(MessageId, new Dictionary<string, string>(Headers), BodySegment) : new OutgoingMessage(MessageId, new Dictionary<string, string>(Headers), Body);
        }
    }
}