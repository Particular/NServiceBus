namespace NServiceBus
{
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class OutboundTransport
    {
        public bool IsDefault { get; }

        public OutboundTransport(bool isDefault)
        {
            IsDefault = isDefault;
        }

        public TransportSendInfrastructure Configure(ReadOnlySettings settings)
        {
            var transportInfrastructure = settings.Get<TransportInfrastructure>();
            return transportInfrastructure.ConfigureSendInfrastructure();
        }
    }
}