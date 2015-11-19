namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Pipeline;
    using PublishOptions = NServiceBus.PublishOptions;

    /// <summary>
    /// Pipeline context for publish operations.
    /// </summary>
    public interface OutgoingPublishContext : OutgoingContext
    {
        /// <summary>
        /// The message to be published.
        /// </summary>
        OutgoingLogicalMessage Message { get; }
    }

    /// <summary>
    /// Pipeline context for publish operations.
    /// </summary>
    public class OutgoingPublishContextImpl : OutgoingContextImpl, OutgoingPublishContext
    {
        /// <summary>
        /// Initializes the context with a parent context.
        /// </summary>
        public OutgoingPublishContextImpl(OutgoingLogicalMessage message, PublishOptions options, BehaviorContext parentContext)
            : base(options.MessageId, options.OutgoingHeaders, parentContext)
        {
            Message = message;
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Extensions.Merge(options.Context);
        }

        /// <summary>
        /// The message to be published.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }
    }
}