namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for send operations.
    /// </summary>
    public class OutgoingSendContext : OutgoingContext, IOutgoingSendContext
    {
        /// <summary>
        /// Creates a new instance of an outgoing send context.
        /// </summary>
        /// <param name="message">The message being sent.</param>
        /// <param name="options">The send options.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingSendContext(OutgoingLogicalMessage message, SendOptions options, IBehaviorContext parentContext)
            : base(options.MessageId, new Dictionary<string, string>(options.OutgoingHeaders), parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            Message = message;

            Merge(options.Context);
        }

        /// <summary>
        /// The message being sent.
        /// </summary>
        public OutgoingLogicalMessage Message { get; }
    }
}