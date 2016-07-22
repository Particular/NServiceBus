namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using System.IO;

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
        /// <param name="bodyStream">The message body stream.</param>
        /// <param name="isPoison"></param>
        public IncomingMessage(string messageId, Dictionary<string, string> headers, Stream bodyStream, bool isPoison = false)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);
            Guard.AgainstNull(nameof(bodyStream), bodyStream);
            Guard.AgainstNull(nameof(headers), headers);

            IsPoison = isPoison;

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
            BodyStream = bodyStream;

            if (!isPoison)
            {
                body = new byte[bodyStream.Length];
                bodyStream.Read(body, 0, body.Length);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public bool IsPoison { get; }

        /// <summary>
        /// The id of the message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// The message body.
        /// </summary>
        public Stream BodyStream { get; private set; }

        /// <summary>
        /// Gets/sets a byte array to the body content of the message.
        /// </summary>
        public byte[] Body
        {
            get { return body; }
            set { UpdateBody(value); }
        }

        /// <summary>
        /// Use this method to update the body if this message.
        /// </summary>
        void UpdateBody(byte[] updatedBody)
        {
            //preserve the original body if needed
            if (body != null && originalBody == null)
            {
                originalBody = new byte[body.Length];
                Buffer.BlockCopy(body, 0, originalBody, 0, body.Length);
            }

            body = updatedBody;
        }

        /// <summary>
        /// Makes sure that the body is reset to the exact state as it was when the message was created.
        /// </summary>
        internal void RevertToOriginalBodyIfNeeded()
        {
            if (originalBody != null)
            {
                body = originalBody;
            }
        }

        byte[] body;
        byte[] originalBody;
    }
}