namespace NServiceBus
{
    using Settings;
    using Transport;

    class OutboundTransport
    {
        public OutboundTransport(bool isDefault)
        {
            IsDefault = isDefault;
        }

        public bool IsDefault { get; }

        public TransportSendInfrastructure Configure(ReadOnlySettings settings)
        {
            var transportInfrastructure = settings.Get<TransportInfrastructure>();
            return transportInfrastructure.ConfigureSendInfrastructure();
        }
    }
}