namespace NServiceBus
{
    using Transport;

    class TransportAddressResolver : ITransportAddressResolver
    {
        TransportInfrastructure transportInfrastructure;

        //TODO: Consider exposing known receive addresses as properties too as a convenience function.
        public TransportAddressResolver(TransportInfrastructure transportInfrastructure)
        {
            this.transportInfrastructure = transportInfrastructure;
        }

        public string ToTransportAddress(QueueAddress queueAddress) => transportInfrastructure.ToTransportAddress(queueAddress);
    }
}