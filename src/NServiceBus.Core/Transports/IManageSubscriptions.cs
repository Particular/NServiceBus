namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Implemented by transports to provide pub/sub capabilities.
    /// </summary>
    public interface IManageSubscriptions
    {
        /// <summary>
        /// Subscribes to the given event.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="context">The current context.</param>
        void Subscribe(Type eventType, ContextBag context);

        /// <summary>
        /// Unsubscribes from the given event.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="context">The current context.</param>
        void Unsubscribe(Type eventType, ContextBag context);
    }
}