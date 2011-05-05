namespace NServiceBus.Gateway.Routing.Endpoints
{
    using Routing;
    using Unicast.Transport;

    public class DefaultEndpointRouter : IRouteMessagesToEndpoints
    {
        public string MainInputAddress { get; set; }

        public string GetDestinationFor(TransportMessage messageToSend)
        {
            return MainInputAddress;
        }
    }
}