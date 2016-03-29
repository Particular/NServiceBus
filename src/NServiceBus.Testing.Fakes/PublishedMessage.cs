namespace NServiceBus.Testing
{
    /// <summary>
    /// Represents an outgoing message. Contains the message itself and it's associated options.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public class PublishedMessage<TMessage> : OutgoingMessage<TMessage, PublishOptions>
    {
        /// <summary>
        /// Creates a new instance for the given message and options.
        /// </summary>
        public PublishedMessage(TMessage message, PublishOptions options) : base(message, options)
        {
        }
    }
}