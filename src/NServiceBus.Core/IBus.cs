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
    }
}
