namespace NServiceBus
{
    using System;

    /// <summary>
    /// Defines the context for IRunWhenBusStartsAndStops
    /// </summary>
    public interface IRunContext
    {
        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        void Subscribe(Type messageType);

        /// <summary>
        /// Subscribes to receive published messages of type T.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        void Subscribe<T>();

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        void Unsubscribe(Type messageType);

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of message to unsubscribe from.</typeparam>
        void Unsubscribe<T>();

        /// <summary>
        ///  Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="options">The options for the publish</param>
        void Publish(object message, PublishOptions options);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <param name="publishOptions">Specific options for this event</param>
        void Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions);

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        void Send(object message, SendOptions options);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <param name="options">The options for the send.</param>
        void Send<T>(Action<T> messageConstructor, SendOptions options);

        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void Reply(object message);

        /// <summary>
        /// Instantiates a message of type T and performs a regular <see cref="Reply(object)"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void Reply<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends the message back to the current bus.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        void SendLocal(object message, SendLocalOptions options);

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <param name="options">The options for the send.</param>
        void SendLocal<T>(Action<T> messageConstructor, SendLocalOptions options);
    }
}