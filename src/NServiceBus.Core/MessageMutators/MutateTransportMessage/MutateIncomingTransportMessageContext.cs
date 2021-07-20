namespace NServiceBus.MessageMutator
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Context class for <see cref="IMutateIncomingTransportMessages" />.
    /// </summary>
    public class MutateIncomingTransportMessageContext : ICancellableContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MutateOutgoingTransportMessageContext" />.
        /// </summary>
        public MutateIncomingTransportMessageContext(ReadOnlyMemory<byte> body, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(body), body);
            Headers = headers;

            // Intentionally assign to field to not set the MessageBodyChanged flag.
            this.body = body;

            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public ReadOnlyMemory<byte> Body
        {
            get
            {
                return body;
            }
            set
            {
                Guard.AgainstNull(nameof(value), value);
                MessageBodyChanged = true;
                body = value;
            }
        }

        /// <summary>
        /// The current incoming headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// A <see cref="CancellationToken"/> to observe.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        ReadOnlyMemory<byte> body;

        internal bool MessageBodyChanged;
    }
}