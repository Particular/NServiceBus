namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Context class for <see cref="IMutateIncomingTransportMessages" />.
    /// </summary>
    public class MutateIncomingTransportMessageContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MutateOutgoingTransportMessageContext" />.
        /// </summary>
        public MutateIncomingTransportMessageContext(byte[] body, IDictionary<string, string> headers)
        {
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(body), body);
            Headers = headers;
            Body = body;
        }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public byte[] Body
        {
            get { return body; }
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
        public IDictionary<string, string> Headers { get; private set; }

        byte[] body;

        internal bool MessageBodyChanged;
    }
}