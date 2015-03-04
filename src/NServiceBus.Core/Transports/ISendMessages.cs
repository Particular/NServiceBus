namespace NServiceBus.Transports
{
    using Unicast;

    /// <summary>
    /// Abstraction of the capability to send messages.
    /// </summary>
    public interface ISendMessages
    {
        /// <summary>
        /// Sends the given <paramref name="message"/>
        /// </summary>
        void Send(OutgoingMessage message, SendOptions sendOptions);
    }

    /// <summary>
    /// The message going out to the transport
    /// </summary>
    public class OutgoingMessage
    {
        /// <summary>
        /// Constructs the message
        /// </summary>
        /// <param name="body"></param>
        public OutgoingMessage(byte[] body)
        {
            Body = body;
        }

        /// <summary>
        /// The body to be sent
        /// </summary>
        public byte[] Body { get; private set; }
    }
}