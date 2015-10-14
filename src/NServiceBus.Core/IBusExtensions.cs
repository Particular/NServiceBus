namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Syntactic sugar for <see cref="IBus"/>.
    /// </summary>
    public static partial class IBusExtensions
    {
        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageType">The type of message to subscribe to.</param>
        public static Task SubscribeAsync(this IBus bus, Type messageType)
        {
            Guard.AgainstNull(nameof(bus), bus);
            Guard.AgainstNull(nameof(messageType), messageType);

            return bus.SubscribeAsync(messageType, new SubscribeOptions());
        }

        /// <summary>
        /// Subscribes to receive published messages of type T.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        public static Task SubscribeAsync<T>(this IBus bus)
        {
            Guard.AgainstNull(nameof(bus), bus);

            return bus.SubscribeAsync(typeof(T), new SubscribeOptions());
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageType">The type of message to subscribe to.</param>
        public static Task UnsubscribeAsync(this IBus bus, Type messageType)
        {
            Guard.AgainstNull(nameof(bus), bus);
            Guard.AgainstNull(nameof(messageType), messageType);

            return bus.UnsubscribeAsync(messageType, new UnsubscribeOptions());
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <typeparam name="T">The type of message to unsubscribe from.</typeparam>
        public static Task UnsubscribeAsync<T>(this IBus bus)
        {
            Guard.AgainstNull(nameof(bus), bus);

            return bus.UnsubscribeAsync(typeof(T), new UnsubscribeOptions());
        }
    }
}