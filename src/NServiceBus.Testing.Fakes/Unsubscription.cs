namespace NServiceBus.Testing
{
    using System;

    /// <summary>
    /// Represents an event subscription cancellation.
    /// </summary>
    public class Unsubscription : OutgoingMessage<Type, UnsubscribeOptions>
    {
        /// <summary>
        /// Creates a new <see cref="Unsubscription" /> instance for the given event type and it's options.
        /// </summary>
        public Unsubscription(Type message, UnsubscribeOptions options) : base(message, options)
        {
        }
    }
}