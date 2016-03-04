namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// The context of the currently processed message within the processing pipeline.
    /// </summary>
    public interface IMessageProcessingContext : IPipelineContext
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
        /// Sends the message to the endpoint which sent the message currently being handled.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">Options for this reply.</param>
        Task Reply(object message, ReplyOptions options);

        /// <summary>
        /// Instantiates a message of type T and performs a regular <see cref="Reply" />.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">Options for this reply.</param>
        Task Reply<T>(Action<T> messageConstructor, ReplyOptions options);

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        Task ForwardCurrentMessageTo(string destination);
    }
}