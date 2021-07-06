namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Provides ways to mutate the outgoing message instance.
    /// </summary>
    public class MutateOutgoingMessageContext : ICancellableContext
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        public MutateOutgoingMessageContext(object outgoingMessage, Dictionary<string, string> outgoingHeaders, object incomingMessage, IDictionary<string, string> incomingHeaders, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(outgoingHeaders), outgoingHeaders);
            Guard.AgainstNull(nameof(outgoingMessage), outgoingMessage);
            OutgoingHeaders = outgoingHeaders;
            this.incomingMessage = incomingMessage;
            this.incomingHeaders = incomingHeaders;
            this.outgoingMessage = outgoingMessage;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// The current outgoing message.
        /// </summary>
        public object OutgoingMessage
        {
            get => outgoingMessage;
            set
            {
                Guard.AgainstNull(nameof(value), value);
                MessageInstanceChanged = true;
                outgoingMessage = value;
            }
        }

        /// <summary>
        /// The current outgoing headers.
        /// </summary>
        public Dictionary<string, string> OutgoingHeaders { get; }

        /// <summary>
        /// A <see cref="CancellationToken"/> to observe.
        /// </summary>
        public CancellationToken CancellationToken { get; }

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
        public bool TryGetIncomingHeaders(out IDictionary<string, string> incomingHeaders)
        {
            incomingHeaders = this.incomingHeaders;
            return incomingHeaders != null;
        }

        IDictionary<string, string> incomingHeaders;
        object incomingMessage;

        internal bool MessageInstanceChanged;

        object outgoingMessage;
    }
}