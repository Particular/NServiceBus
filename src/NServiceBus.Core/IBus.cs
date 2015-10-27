namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a bus to be used with NServiceBus.
    /// </summary>
    public partial interface IBus : ISendOnlyBus
    {
        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        Task SubscribeAsync(Type eventType, SubscribeOptions options);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe from.</param>
        /// <param name="options">Options for the unsubscribe operation.</param>
        Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options);

        /// <summary>
        /// Creates a <see cref="IBusContext"/> which can be used to access several bus operations like send, publish, subscribe and more.
        /// </summary>
        /// <returns>a new <see cref="IBusContext"/> to which all operations performed on it are scoped.</returns>
        IBusContext CreateSendContext();
    }
}
