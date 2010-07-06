using NServiceBus.Unicast.Transport;

namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Maps between the messages requested to be sent to the physical transport message that will be sent.
    /// </summary>
    public interface IMapOutgoingTransportMessages
    {
        void MapOutgoing(IMessage[] messages, TransportMessage transportMessage);
    }
}
