namespace NServiceBus.Gateway.Sending
{
    using Routing;
    using Unicast.Transport;

    public interface IForwardMessagesToSites
    {
        void Forward(TransportMessage message,Site targetSite);
    }
}