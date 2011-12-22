using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Queuing
{
    /// <summary>
    /// Abstraction of a the capability to send messages
    /// </summary>
    public interface ISendMessages
    {
        /// <summary>
        /// Sends the given message to the address.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="address"></param>
        void Send(TransportMessage message, Address address);
    }
}