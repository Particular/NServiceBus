namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// The context for the current message handling pipeline.
    /// </summary>
    public interface IPipelineContext : IExtendable
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        Task Send(object message, SendOptions options);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        Task Send<T>(Action<T> messageConstructor, SendOptions options);

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        Task Publish(object message, PublishOptions options);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="publishOptions">Specific options for this event.</param>
        Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions);
    }
}