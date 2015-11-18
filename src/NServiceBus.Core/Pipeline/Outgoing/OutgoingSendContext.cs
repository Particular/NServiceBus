﻿namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for send operations.
    /// </summary>
    public class OutgoingSendContext : OutgoingContext
    {
        /// <summary>
        /// Initializes the context with a parent context.
        /// </summary>
        public OutgoingSendContext(OutgoingLogicalMessage message, SendOptions options, BehaviorContext parentContext)
            : base(options.MessageId, options.OutgoingHeaders, parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            Message = message;

            Merge(options.Context);
        }

        /// <summary>
        /// The message beeing sent.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }
    }
}