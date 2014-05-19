namespace NServiceBus.Unicast.Transport
{
    using Messages;

    /// <summary>
    /// Extensions to make the usage if control messages easier
    /// </summary>
    public static class TransportMessageExtensions
    {
        /// <summary>
        /// True if the transport message is a control message
        /// </summary>
        public static bool IsControlMessage(this TransportMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader);
        }


        public static bool IsControlMessage(this LogicalMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader);
        }
    }
}