namespace NServiceBus.Gateway.Routing.Endpoints
{
    using System.Configuration;
    using HeaderManagement;
    using Unicast.Transport;

    public class LegacyEndpointRouter : IRouteMessagesToEndpoints
    {
        public Address GetDestinationFor(TransportMessage messageToSend)
        {
            var routeTo = Headers.RouteTo.Replace(HeaderMapper.NServiceBus + Headers.HeaderName + ".", "");
          
            if (messageToSend.Headers.ContainsKey(routeTo))
                return Address.Parse(messageToSend.Headers[routeTo]);

            return DefaultDestinationAddress;
        }

        Address DefaultDestinationAddress
        {
            get
            {
                if(defaultDestinationAddress == null)
                {
                    var outputQueue = ConfigurationManager.AppSettings["OutputQueue"];

                    if (string.IsNullOrEmpty(outputQueue))
                        throw new ConfigurationErrorsException("Required setting 'OutputQueue' is missing");

                    defaultDestinationAddress = Address.Parse(outputQueue);
                }
                return defaultDestinationAddress;
            }
        }

        Address defaultDestinationAddress;

    }
}