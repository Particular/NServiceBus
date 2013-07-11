namespace NServiceBus.Transports.ActiveMQ
{
    using Apache.NMS;

    /// <summary>
    /// Extensions to make the usage if control messages easier
    /// </summary>
    internal static class MessageExtensions
    {
        /// <summary>
        /// True if the transport message is a control message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsControlMessage(this IMessage message)
        {
            return message.Properties != null &&
                   message.Properties.Contains(Headers.ControlMessageHeader);
        }
    }
}