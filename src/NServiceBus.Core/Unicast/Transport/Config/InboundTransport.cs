namespace NServiceBus
{
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class InboundTransport
    {
        public TransportReceiveInfrastructure Configure(ReadOnlySettings settings)
        {
            var transportInfrastructure = settings.Get<TransportInfrastructure>();
            return transportInfrastructure.ConfigureReceiveInfrastructure();
        }        
    }
}