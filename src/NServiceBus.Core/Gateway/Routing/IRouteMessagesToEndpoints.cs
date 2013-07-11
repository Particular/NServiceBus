namespace NServiceBus.Gateway.Routing
{
    public interface IRouteMessagesToEndpoints
    {
        Address GetDestinationFor(TransportMessage messageToSend);
    }
}