namespace NServiceBus.Testing
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Represents an outgoing message. Contains the message itself and its associated options.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <typeparam name="TOptions">The options type of the message.</typeparam>
    public class OutgoingMessage<TMessage, TOptions> where TOptions : ExtendableOptions
    {
        /// <summary>
        /// Creates a new instance for the given message and options.
        /// </summary>
        protected OutgoingMessage(TMessage message, TOptions options)
        {
            Message = message;
            Options = options;
        }

        /// <summary>
        /// The outgoing message.
        /// </summary>
        public TMessage Message { get; }

        /// <summary>
        /// The options of the outgoing message.
        /// </summary>
        public TOptions Options { get; }
    }
}