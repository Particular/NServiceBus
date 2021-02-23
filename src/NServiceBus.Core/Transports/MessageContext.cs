﻿namespace NServiceBus.Transport
{
    using System.Collections.Generic;
    using Extensibility;

    /// <summary>
    /// Allows the transport to pass relevant info to the pipeline.
    /// </summary>
    public partial class MessageContext : IExtendable
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        /// <param name="transportMessageId">Native message id.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="body">The message body.</param>
        /// <param name="transportTransaction">Transaction (along with connection if applicable) used to receive the message.</param>
        /// <param name="context">A <see cref="ContextBag" /> which can be used to extend the current object.</param>
        public MessageContext(string transportMessageId, Dictionary<string, string> headers, byte[] body, TransportTransaction transportTransaction, ContextBag context)
        {
            Guard.AgainstNullAndEmpty(nameof(transportMessageId), transportMessageId);
            Guard.AgainstNull(nameof(body), body);
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(transportTransaction), transportTransaction);
            Guard.AgainstNull(nameof(context), context);

            Headers = headers;
            Body = body;
            TransportMessageId = transportMessageId;
            Extensions = context;
            TransportTransaction = transportTransaction;
        }

        /// <summary>
        /// The native id of the message.
        /// </summary>
        public string TransportMessageId { get; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The message body.
        /// </summary>
        public byte[] Body { get; }

        /// <summary>
        /// Transaction (along with connection if applicable) used to receive the message.
        /// </summary>
        public TransportTransaction TransportTransaction { get; }

        /// <summary>
        /// A <see cref="ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; }
    }
}