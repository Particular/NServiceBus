namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    /// <summary>
    /// The context of the currently processed message within the processing pipeline.
    /// </summary>
    public interface IMessageProcessingContext
    {
        /// <summary>
        /// The Id of the currently processed message.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// The address of the endpoint that sent the current message being handled.
        /// </summary>
        string ReplyToAddress { get; }

        /// <summary>
        /// Gets the list of key/value pairs found in the header of the message.
        /// </summary>
        IReadOnlyDictionary<string, string> MessageHeaders { get; }

        /// <summary>
        /// A <see cref="ContextBag"/> which can be used for extensibility.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        ContextBag Extensions { get; }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        Task SendAsync(object message, SendOptions options);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        Task SendAsync<T>(Action<T> messageConstructor, SendOptions options);

        /// <summary>
        ///  Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        Task PublishAsync(object message, PublishOptions options);

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="publishOptions">Specific options for this event.</param>
        Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions);

        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">Options for this reply.</param>
        Task ReplyAsync(object message, ReplyOptions options);

        ///  <summary>
        /// Instantiates a message of type T and performs a regular <see cref="ReplyAsync"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">Options for this reply.</param>
        Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options);

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        Task ForwardCurrentMessageToAsync(string destination);
    }
}