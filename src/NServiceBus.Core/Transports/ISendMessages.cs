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
}