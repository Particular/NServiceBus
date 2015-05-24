namespace NServiceBus.Saga
{
    using System;

    /// <summary>
    /// Defines a context for IProcessTimeouts
    /// </summary>
    public interface ITimeoutContext
    {
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