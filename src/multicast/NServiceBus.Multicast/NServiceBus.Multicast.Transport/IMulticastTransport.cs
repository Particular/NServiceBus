using NServiceBus.Unicast.Transport;

namespace NServiceBus.Multicast.Transport
{
    public interface IMulticastTransport : ITransport
    {
        void Subscribe(string address);

        void Unsubscribe(string address);

        void Publish(TransportMessage message, string address);
    }
}
