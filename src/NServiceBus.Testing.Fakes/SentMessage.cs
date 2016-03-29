namespace NServiceBus.Testing
{
    /// <summary>
    /// Represents an outgoing message. Contains the message itself and it's associated options.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public class SentMessage<TMessage> : OutgoingMessage<TMessage, SendOptions>
    {
        /// <summary>
        /// Creates a new instance for the given message and options.
        /// </summary>
        public SentMessage(TMessage message, SendOptions options) : base(message, options)
        {
        }
    }
}