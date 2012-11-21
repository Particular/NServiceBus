namespace NServiceBus.Gateway.Routing.Endpoints
{
    public class DefaultEndpointRouter : IRouteMessagesToEndpoints
    {
        public Address MainInputAddress { get; set; }

        public Address GetDestinationFor(TransportMessage messageToSend)
        {
            return MainInputAddress;
        }
    }
}