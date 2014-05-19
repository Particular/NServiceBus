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
}
