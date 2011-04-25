namespace NServiceBus.Gateway.Routing.Endpoints
{
    using System.Configuration;
    using Channels.Http;
    using Unicast.Transport;

    public class LegacyEndpointRouter : IRouteMessagesToEndpoints
    {
        public LegacyEndpointRouter()
        {
            defaultDestinationAddress = ConfigurationManager.AppSettings["OutputQueue"];

            if (string.IsNullOrEmpty(defaultDestinationAddress))
                throw new ConfigurationErrorsException("Required setting 'OutputQueue' is missing");
        }

        public string GetDestinationFor(TransportMessage messageToSend)
        {
            //todo - figure out why we use a funny name for this header
            var routeTo = Headers.RouteTo.Replace(HeaderMapper.NServiceBus + Headers.HeaderName + ".", "");
          
            if (messageToSend.Headers.ContainsKey(routeTo))
                return  messageToSend.Headers[routeTo];

            return defaultDestinationAddress;
        }

        readonly string defaultDestinationAddress;
    }
}