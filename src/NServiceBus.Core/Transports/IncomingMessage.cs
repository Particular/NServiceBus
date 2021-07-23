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
        /// <param name="nativeMessageId">The native message ID.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="body">The message body.</param>
        public IncomingMessage(string nativeMessageId, Dictionary<string, string> headers, ReadOnlyMemory<byte> body)
        {
            Guard.AgainstNullAndEmpty(nameof(nativeMessageId), nativeMessageId);
            Guard.AgainstNull(nameof(body), body);
            Guard.AgainstNull(nameof(headers), headers);

            if (headers.TryGetValue(NServiceBus.Headers.MessageId, out var originalMessageId) && !string.IsNullOrEmpty(originalMessageId))
            {
                MessageId = originalMessageId;
            }
            else
            {
                MessageId = nativeMessageId;

                headers[NServiceBus.Headers.MessageId] = nativeMessageId;
            }

            NativeMessageId = nativeMessageId;

            Headers = headers;

            Body = body;
        }

        /// <summary>
        /// The message ID.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// The native message ID.
        /// </summary>
        public string NativeMessageId { get; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets/sets a byte array to the body content of the message.
        /// </summary>
        public ReadOnlyMemory<byte> Body { get; private set; }

        /// <summary>
        /// Use this method to update the body if this message.
        /// </summary>
        internal void UpdateBody(ReadOnlyMemory<byte> updatedBody)
        {
            if (originalBody == null)
            {
                originalBody = Body;
            }

            Body = updatedBody;
        }

        /// <summary>
        /// Makes sure that the body is reset to the exact state as it was when the message was created.
        /// </summary>
        internal void RevertToOriginalBodyIfNeeded()
        {
            if (originalBody != null)
            {
                Body = originalBody.Value;
            }
        }

        ReadOnlyMemory<byte>? originalBody;
    }
}