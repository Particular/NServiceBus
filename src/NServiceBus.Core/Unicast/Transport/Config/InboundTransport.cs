namespace NServiceBus
{
    using Settings;
    using Transports;

    class InboundTransport
    {
        public TransportReceiveInfrastructure Configure(ReadOnlySettings settings)
        {
            var transportInfrastructure = settings.Get<TransportInfrastructure>();
            return transportInfrastructure.ConfigureReceiveInfrastructure();
        }
    }
}