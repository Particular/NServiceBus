namespace NServiceBus.Gateway.Routing
{
    public interface IRouteMessagesToEndpoints
    {
        // ReSharper disable once UnusedParameter.Global        
        Address GetDestinationFor(TransportMessage messageToSend);
    }
}