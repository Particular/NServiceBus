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
}