namespace NServiceBus.Gateway.Sending
{
    using Routing;

    public interface IForwardMessagesToSites
    {
        void Forward(TransportMessage message, Site targetSite);
    }
}