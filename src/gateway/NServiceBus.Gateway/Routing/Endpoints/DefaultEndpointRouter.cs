namespace NServiceBus.Gateway.Routing.Endpoints
{
    using Routing;
    using Unicast.Transport;

    public class DefaultEndpointRouter : IRouteMessagesToEndpoints
    {
        public Address MainInputAddress { get; set; }

        public Address GetDestinationFor(TransportMessage messageToSend)
        {
            return MainInputAddress;
        }
    }
}