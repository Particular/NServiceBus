namespace NServiceBus.Gateway.Routing
{
    using Unicast.Transport;

    public interface IRouteMessagesToEndpoints
    {
        string GetDestinationFor(TransportMessage messageToSend);
    }
}