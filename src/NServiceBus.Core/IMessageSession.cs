namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A session which provides basic message operations.
    /// </summary>
    public interface IMessageSession
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        Task Send(object message, SendOptions options);

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        Task Publish(object message, PublishOptions options);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        Task Subscribe(Type eventType, SubscribeOptions options);

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        Task Unsubscribe(Type eventType, UnsubscribeOptions options);
    }
}