#nullable enable

namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    /// <summary>
    /// Provides ways to mutate the outgoing message instance.
    /// </summary>
    public class MutateOutgoingMessageContext : ICancellableContext
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        public MutateOutgoingMessageContext(object outgoingMessage, Dictionary<string, string> outgoingHeaders, object? incomingMessage, IReadOnlyDictionary<string, string>? incomingHeaders, CancellationToken cancellationToken = default)
        {
            Guard.ThrowIfNull(outgoingHeaders);
            Guard.ThrowIfNull(outgoingMessage);
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
                Guard.ThrowIfNull(value);
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
        public bool TryGetIncomingMessage([NotNullWhen(true)] out object? incomingMessage)
        {
            incomingMessage = this.incomingMessage;
            return incomingMessage != null;
        }

        /// <summary>
        /// Gets the incoming headers that initiated the current send if it exists.
        /// </summary>
        public bool TryGetIncomingHeaders([NotNullWhen(true)] out IReadOnlyDictionary<string, string>? incomingHeaders)
        {
            incomingHeaders = this.incomingHeaders;
            return incomingHeaders != null;
        }

        readonly IReadOnlyDictionary<string, string>? incomingHeaders;
        readonly object? incomingMessage;

        internal bool MessageInstanceChanged;

        object outgoingMessage;
    }
}