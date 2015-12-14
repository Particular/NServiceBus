namespace NServiceBus
{
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class OutboundTransport
    {
        public TransportDefinition Definition { get; }
        public bool IsDefault { get; }

        public OutboundTransport(TransportDefinition definition, bool isDefault)
        {
            Definition = definition;
            IsDefault = isDefault;
        }

        public TransportSendingConfigurationResult Configure(ReadOnlySettings settings)
        {
            var connectionString = settings.Get<TransportConnectionString>().GetConnectionStringOrRaiseError(Definition);
            var context = new TransportSendingConfigurationContext(settings, connectionString);
            return Definition.ConfigureForSending(context);
        }
    }
}