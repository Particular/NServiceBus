namespace NServiceBus
{
    using System;
    using System.Threading;
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
        /// <param name="sendOptions">The options for the send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="sendOptions">The options for the send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="publishOptions">The options for the publish.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="publishOptions">Specific options for this event.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="subscribeOptions">Options for the subscribe.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="unsubscribeOptions">Options for the subscribe.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default);
    }
}