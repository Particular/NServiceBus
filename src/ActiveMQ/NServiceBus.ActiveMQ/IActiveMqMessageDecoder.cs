namespace NServiceBus.Transports.ActiveMQ
{
    using Apache.NMS;

    public interface IActiveMqMessageDecoder
    {
        /// <summary>
        /// Decodes a given message.
        /// </summary>
        /// <param name="message">The message to decode</param>
        /// <returns>true if decoded; false otherwise;</returns>
        bool Decode(TransportMessage transportMessage, IMessage message);
    }
}