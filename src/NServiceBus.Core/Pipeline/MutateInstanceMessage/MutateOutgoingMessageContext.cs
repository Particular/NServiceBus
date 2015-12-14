namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides ways to mutate the outgoing message instance.
    /// </summary>
    public class MutateOutgoingMessageContext
    {

        object outgoingMessage;
        /// <summary>
        /// Initializes the context.
        /// </summary>
        public MutateOutgoingMessageContext(object outgoingMessage, IDictionary<string, string> outgoingHeaders, object incomingMessage, IReadOnlyDictionary<string, string> incomingHeaders)
        {
            Guard.AgainstNull(nameof(outgoingHeaders), outgoingHeaders);
            Guard.AgainstNull(nameof(outgoingMessage), outgoingMessage);
            OutgoingHeaders = outgoingHeaders;
            this.incomingMessage = incomingMessage;
            this.incomingHeaders = incomingHeaders;
            this.outgoingMessage = outgoingMessage;
        }

        /// <summary>
        /// The current outgoing message.
        /// </summary>
        public object OutgoingMessage
        {
            get
            {
                return outgoingMessage;
            }
            set
            {
                Guard.AgainstNull(nameof(value), value);
                MessageInstanceChanged = true;
                outgoingMessage = value;
            }
        }

        internal bool MessageInstanceChanged;
        object incomingMessage;
        IReadOnlyDictionary<string, string> incomingHeaders;

        /// <summary>
        /// The current outgoing headers.
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders { get; private set; }

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
    }
}