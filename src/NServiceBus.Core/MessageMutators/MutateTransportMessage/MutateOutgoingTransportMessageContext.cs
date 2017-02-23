namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Context class for <see cref="IMutateOutgoingTransportMessages" />.
    /// </summary>
    public class MutateOutgoingTransportMessageContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MutateOutgoingTransportMessageContext" />.
        /// </summary>
        public MutateOutgoingTransportMessageContext(byte[] outgoingBody, object outgoingMessage, Dictionary<string, string> outgoingHeaders, object incomingMessage, IReadOnlyDictionary<string, string> incomingHeaders)
        {
            Guard.AgainstNull(nameof(outgoingHeaders), outgoingHeaders);
            Guard.AgainstNull(nameof(outgoingBody), outgoingBody);
            Guard.AgainstNull(nameof(outgoingMessage), outgoingMessage);

            OutgoingHeaders = outgoingHeaders;
            // Intentionally assign to field to not set the MessageBodyChanged flag.
            this.outgoingBody = outgoingBody;
            OutgoingMessage = outgoingMessage;
            this.incomingHeaders = incomingHeaders;
            this.incomingMessage = incomingMessage;
        }

        /// <summary>
        /// The current outgoing message.
        /// </summary>
        public object OutgoingMessage { get; }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public byte[] OutgoingBody
        {
            get { return outgoingBody; }
            set
            {
                Guard.AgainstNull(nameof(value), value);
                MessageBodyChanged = true;
                outgoingBody = value;
            }
        }

        /// <summary>
        /// The current outgoing headers.
        /// </summary>
        public Dictionary<string, string> OutgoingHeaders { get; }

        /// <summary>
        /// Gets the incoming message that initiated the current send if it exists.
        /// </summary>
        public bool TryGetIncomingMessage(out object incomingMessage)
        {
            incomingMessage = this.incomingMessage;
            return incomingMessage != null;
        }

        /// <summary>
        /// Gets the incoming headers that initiated the current send if it exists.
        /// </summary>
        public bool TryGetIncomingHeaders(out IReadOnlyDictionary<string, string> incomingHeaders)
        {
            incomingHeaders = this.incomingHeaders;
            return incomingHeaders != null;
        }

        IReadOnlyDictionary<string, string> incomingHeaders;
        object incomingMessage;

        internal bool MessageBodyChanged;
        byte[] outgoingBody;
    }
}