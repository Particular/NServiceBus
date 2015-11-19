namespace NServiceBus.Routing
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Provides context for unsubscribe requests.
    /// </summary>
    public interface UnsubscribeContext : BehaviorContext
    {
        /// <summary>
        /// The type of the event.
        /// </summary>
        Type EventType { get; }
    }

    /// <summary>
    /// Provides context for unsubscribe requests.
    /// </summary>
    class UnsubscribeContextImpl : BehaviorContextImpl, UnsubscribeContext
    {
        /// <summary>
        /// Initializes the context with the given event type and parent context.
        /// </summary>
        public UnsubscribeContextImpl(BehaviorContextImpl parentContext, Type eventType, UnsubscribeOptions options)
            : base(parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Merge(options.Context);

            EventType = eventType;
        }

        /// <summary>
        /// The type of the event.
        /// </summary>
        public Type EventType { get; private set; }
    }
}