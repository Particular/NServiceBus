namespace NServiceBus.Gateway.Sending
{
    using Routing;
    using Unicast.Transport;

    public interface ISendMessagesToSites
    {
        void Send(Site targetSite, TransportMessage message);
    }
}