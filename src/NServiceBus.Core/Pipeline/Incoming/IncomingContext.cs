namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using NServiceBus.Extensibility;

    /// <summary>
    /// The abstract base context for everything after the transport receive phase.
    /// </summary>
    public abstract class IncomingContext : BehaviorContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IncomingContext" />.
        /// </summary>
        protected IncomingContext(string messageId, string replyToAddress, IReadOnlyDictionary<string, string> headers, BehaviorContext parentContext)
            : base(parentContext)
        {

            this.MessageId = messageId;
            this.ReplyToAddress = replyToAddress;
            this.MessageHeaders = headers;
        }

        /// <inheritdoc />
        public ContextBag Extensions => this;

        /// <inheritdoc />
        public string MessageId { get; }

        /// <inheritdoc />
        public string ReplyToAddress { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }
    }
}