﻿namespace NServiceBus.Outbox
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    ///     Outgoing message operation.
    /// </summary>
    public class TransportOperation
    {
        /// <summary>
        ///     Creates a new instance of a <see cref="TransportOperation" />.
        /// </summary>
        /// <param name="messageId">The identifier of the outgoing message.</param>
        /// <param name="options">The sending options.</param>
        /// <param name="body">The message body.</param>
        /// <param name="headers">The message headers.</param>
        /// .
        public TransportOperation(string messageId, Dictionary<string, string> options, Stream body, Dictionary<string, string> headers)
        {
            Guard.AgainstNullAndEmpty("messageId", messageId);

            MessageId = messageId;
            Options = options;
            Body = body;
            Headers = headers;
        }

        /// <summary>
        ///     Gets the identifier of the outgoing message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        ///     Sending options.
        /// </summary>
        public Dictionary<string, string> Options { get; private set; }

        /// <summary>
        ///     Gets a byte array to the body content of the outgoing message.
        /// </summary>
        public Stream Body { get; internal set; }

        /// <summary>
        ///     Gets outgoing message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }
    }
}