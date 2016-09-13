namespace NServiceBus.Features
{
    using System.Collections.Concurrent;
    using Routing;
    using Transport;

    class TransportAddressTranslator
    {
        public TransportAddressTranslator(TransportInfrastructure transportInfrastructure)
        {
            this.transportInfrastructure = transportInfrastructure;
        }

        public string ToTransportAddress(EndpointInstance remoteEndpointInstance)
        {
            return cache.GetOrAdd(remoteEndpointInstance, Translate);
        }

        string Translate(EndpointInstance endpointInstance)
        {
            return transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(endpointInstance));
        }

        ConcurrentDictionary<EndpointInstance, string> cache = new ConcurrentDictionary<EndpointInstance, string>();
        TransportInfrastructure transportInfrastructure;
    }
}