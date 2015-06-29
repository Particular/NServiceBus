namespace NServiceBus.Routing
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Provides context for unsubscribe requests.
    /// </summary>
    public class UnsubscribeContext : BehaviorContext
    {
        /// <summary>
        /// Initializes the context with the given event type and parent context.
        /// </summary>
        public UnsubscribeContext(BehaviorContext parentContext, Type eventType, UnsubscribeOptions options)
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