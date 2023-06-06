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
        public MutateIncomingMessageContext(object message, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            Guard.ThrowIfNull(headers);
            Guard.ThrowIfNull(message);
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
                Guard.ThrowIfNull(value);
                MessageInstanceChanged = true;
                message = value;
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

        object message;

        internal bool MessageInstanceChanged;
    }
}