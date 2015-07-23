namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides the subset of bus operations that is applicable for a send only bus.
    /// </summary>
    public partial interface ISendOnlyBus : IDisposable
    {
       /// <summary>
        ///  Publish the message to subscribers.
       /// </summary>
       /// <param name="message">The message to publish.</param>
       /// <param name="options">The options for the publish.</param>
        void Publish(object message,PublishOptions options);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="publishOptions">Specific options for this event.</param>
        void Publish<T>(Action<T> messageConstructor,PublishOptions publishOptions);

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        void Send(object message, SendOptions options);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        void Send<T>(Action<T> messageConstructor, SendOptions options);
    }
}
