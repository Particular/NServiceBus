using NServiceBus.Unicast.Transport;

namespace NServiceBus.Multicast.Transport
{
    /// <summary>
    /// Transport used by the Multicast Bus
    /// </summary>
    public interface IMulticastTransport : ITransport
    {
        /// <summary>
        /// Subscribes to the given topic.
        /// </summary>
        /// <param name="topic"></param>
        void Subscribe(string topic);

        /// <summary>
        /// Unsubscribes from the given topic.
        /// </summary>
        /// <param name="topic"></param>
        void Unsubscribe(string topic);

        /// <summary>
        /// Publishes the given message on the given topic.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="topic"></param>
        void Publish(TransportMessage message, string topic);
    }
}
