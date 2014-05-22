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
        /// <param name="message"></param>
        /// <param name="sendOptions"></param>
        void Send(TransportMessage message, SendOptions sendOptions);
    }
}