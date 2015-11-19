namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Pipeline;
    using ReplyOptions = NServiceBus.ReplyOptions;

    /// <summary>
    /// 
    /// </summary>
    public interface OutgoingReplyContext : OutgoingContext
    {
        /// <summary>
        /// The reply message.
        /// </summary>
        OutgoingLogicalMessage Message { get; }
    }

    /// <summary>
    /// Pipeline context for reply operations.
    /// </summary>
    public class OutgoingReplyContextImpl : OutgoingContextImpl, OutgoingReplyContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OutgoingReplyContextImpl" />.
        /// </summary>
        public OutgoingReplyContextImpl(OutgoingLogicalMessage message, ReplyOptions options, BehaviorContextImpl parentContext)
            : base(options.MessageId, options.OutgoingHeaders, parentContext)
        {
            Message = message;
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Merge(options.Context);
        }

        /// <summary>
        /// The reply message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }
    }
}