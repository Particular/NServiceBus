namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Provides ways to mutate the outgoing message instance.
    /// </summary>
    public class MutateIncomingMessageContext : ICancellableContext
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        public MutateIncomingMessageContext(object message, IDictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(message), message);
            Headers = headers;
            this.message = message;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// The current incoming message.
        /// </summary>
        public object Message
        {
            get => message;
            set
            {
                Guard.AgainstNull(nameof(value), value);
                MessageInstanceChanged = true;
                message = value;
            }
        }

        /// <summary>
        /// The current incoming headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; }

        /// <summary>
        /// A <see cref="CancellationToken"/> to observe.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        object message;

        internal bool MessageInstanceChanged;
    }
}