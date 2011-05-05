namespace NServiceBus.Gateway
{
    using Routing;
    using Unicast.Transport;

    public interface ITransmittMessages
    {
        void Send(Site targetSite, TransportMessage message);
    }
}