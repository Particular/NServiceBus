namespace NServiceBus.Routing
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Provides context for subscription requests.
    /// </summary>
    public class SubscribeContext : BehaviorContext
    {
        /// <summary>
        /// Initializes the context with the given event type and parent context.
        /// </summary>
        public SubscribeContext(BehaviorContext parentContext, Type eventType, SubscribeOptions options)
            : base(parentContext)
        {
            Guard.AgainstNull("parentContext", parentContext);
            Guard.AgainstNull("eventType", eventType);
            Guard.AgainstNull("options", options);

            parentContext.Merge(options.Context);

            EventType = eventType;
        }

        /// <summary>
        /// The type of the event.
        /// </summary>
        public Type EventType { get; private set; }
    }
}