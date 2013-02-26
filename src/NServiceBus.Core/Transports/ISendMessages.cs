namespace NServiceBus.Transports
{
    /// <summary>
    /// Abstraction of the capability to send messages.
    /// </summary>
    public interface ISendMessages
    {
        /// <summary>
        /// Sends the given <paramref name="message"/> to the <paramref name="address"/>.
        /// </summary>
        /// <param name="message"><see cref="TransportMessage"/> to send.</param>
        /// <param name="address">Destination <see cref="Address"/>.</param>
        void Send(TransportMessage message, Address address);
    }
}