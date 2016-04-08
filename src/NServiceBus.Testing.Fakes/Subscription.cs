namespace NServiceBus.Testing
{
    using System;

    /// <summary>
    /// Represents an event subscription.
    /// </summary>
    public class Subscription : OutgoingMessage<Type, SubscribeOptions>
    {
        /// <summary>
        /// Creates a new <see cref="Subscription" /> instance for the given event type and it's options.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="options"></param>
        public Subscription(Type message, SubscribeOptions options) : base(message, options)
        {
        }
    }
}