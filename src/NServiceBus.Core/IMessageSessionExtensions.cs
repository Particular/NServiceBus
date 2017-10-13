namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Syntactic sugar for <see cref="IMessageSession" />.
    /// </summary>
    public static class IMessageSessionExtensions
    {
        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="session">Object being extended.</param>
        /// <param name="messageType">The type of message to subscribe to.</param>
        public static Task Subscribe(this IMessageSession session, Type messageType)
        {
            Guard.AgainstNull(nameof(session), session);
            Guard.AgainstNull(nameof(messageType), messageType);

            return session.Subscribe(messageType, new SubscribeOptions());
        }

        /// <summary>
        /// Subscribes to receive published messages of type T.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="session">Object being extended.</param>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        public static Task Subscribe<T>(this IMessageSession session)
        {
            Guard.AgainstNull(nameof(session), session);

            return session.Subscribe(typeof(T), new SubscribeOptions());
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="session">Object being extended.</param>
        /// <param name="messageType">The type of message to subscribe to.</param>
        public static Task Unsubscribe(this IMessageSession session, Type messageType)
        {
            Guard.AgainstNull(nameof(session), session);
            Guard.AgainstNull(nameof(messageType), messageType);

            return session.Unsubscribe(messageType, new UnsubscribeOptions());
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="session">Object being extended.</param>
        /// <typeparam name="T">The type of message to unsubscribe from.</typeparam>
        public static Task Unsubscribe<T>(this IMessageSession session)
        {
            Guard.AgainstNull(nameof(session), session);

            return session.Unsubscribe(typeof(T), new UnsubscribeOptions());
        }
    }
}