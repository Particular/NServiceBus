namespace NServiceBus.Gateway.Routing
{
    using Unicast.Transport;

    public interface IRouteMessagesToEndpoints
    {
        Address GetDestinationFor(TransportMessage messageToSend);
    }
}