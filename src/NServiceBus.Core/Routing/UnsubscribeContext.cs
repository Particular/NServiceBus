namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Provides context for unsubscribe requests.
    /// </summary>
    public class UnsubscribeContext : BehaviorContext, IUnsubscribeContext
    {
        /// <summary>
        /// Creates a new instance of an unsubscribe context.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="options">The unsubscribe options.</param>
        public UnsubscribeContext(IBehaviorContext parentContext, Type eventType, UnsubscribeOptions options)
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