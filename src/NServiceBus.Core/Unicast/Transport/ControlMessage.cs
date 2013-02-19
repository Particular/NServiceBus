namespace NServiceBus.Unicast.Transport
{
    /// <summary>
    /// Helper for creating control messages
    /// </summary>
    public static class ControlMessage
    {
        /// <summary>
        /// Creates Transport Message
        /// </summary>
        /// <returns>Transport Message</returns>
        public static TransportMessage Create(Address replyToAddress)
        {
            var transportMessage = new TransportMessage
                                       {
                                           ReplyToAddress = replyToAddress,
                                           Recoverable = true,
                                       };
            transportMessage.Headers.Add(Headers.ControlMessageHeader, true.ToString());

            return transportMessage;
        }
    }

    /// <summary>
    /// Extensions to make the usage if control messages easier
    /// </summary>
    public static class TransportMessageExtensions
    {
        /// <summary>
        /// True if the transport message is a control message
        /// </summary>
        /// <param name="transportMessage"></param>
        /// <returns></returns>
        public static bool IsControlMessage(this TransportMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader);
        }

    }
}
