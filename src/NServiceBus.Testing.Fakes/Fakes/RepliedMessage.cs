namespace NServiceBus.Testing
{
    /// <summary>
    /// Represents an outgoing message. Contains the message itself and it's associated options.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public class RepliedMessage<TMessage> : OutgoingMessage<TMessage, ReplyOptions>
    {
        /// <summary>
        /// Creates a new instance for the given message and options.
        /// </summary>
        public RepliedMessage(TMessage message, ReplyOptions options) : base(message, options)
        {
        }
    }
}