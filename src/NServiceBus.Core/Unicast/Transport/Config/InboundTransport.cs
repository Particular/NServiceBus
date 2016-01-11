namespace NServiceBus
{
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class InboundTransport
    {
        public TransportReceiveInfrastructure Configure(ReadOnlySettings settings)
        {
            var transportInfrastructure = settings.Get<TransportInfrastructure>();
            var connectionString = settings.Get<TransportConnectionString>().GetConnectionStringOrRaiseError(transportInfrastructure);
            return transportInfrastructure.ConfigureReceiveInfrastructure(connectionString);
        }        
    }
}