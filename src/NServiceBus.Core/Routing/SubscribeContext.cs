namespace NServiceBus.Routing
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Provides context for subscription requests.
    /// </summary>
    public interface SubscribeContext : BehaviorContext
    {
        /// <summary>
        /// The type of the event.
        /// </summary>
        Type EventType { get; }
    }

    /// <summary>
    /// Provides context for subscription requests.
    /// </summary>
    class SubscribeContextImpl : BehaviorContextImpl, SubscribeContext
    {
        /// <summary>
        /// Initializes the context with the given event type and parent context.
        /// </summary>
        public SubscribeContextImpl(BehaviorContextImpl parentContext, Type eventType, SubscribeOptions options)
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