namespace NServiceBus.Transports
{
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