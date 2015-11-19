namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for send operations.
    /// </summary>
    public interface OutgoingSendContext : OutgoingContext
    {
        /// <summary>
        /// The message beeing sent.
        /// </summary>
        OutgoingLogicalMessage Message { get; }
    }

    /// <summary>
    /// Pipeline context for send operations.
    /// </summary>
    class OutgoingSendContextImpl : OutgoingContextImpl, OutgoingSendContext
    {
        /// <summary>
        /// Initializes the context with a parent context.
        /// </summary>
        public OutgoingSendContextImpl(OutgoingLogicalMessage message, SendOptions options, BehaviorContextImpl parentContext)
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