namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Provides context for subscription requests.
    /// </summary>
    public class SubscribeContext : BehaviorContext, ISubscribeContext
    {
        /// <summary>
        /// Creates a new instance of a subscribe context.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="options">The subscribe options.</param>
        public SubscribeContext(IBehaviorContext parentContext, Type eventType, SubscribeOptions options)
            : base(parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Extensions.Merge(options.Context);

            EventType = eventType;
        }

        /// <summary>
        /// The type of the event.
        /// </summary>
        public Type EventType { get; }
    }
}