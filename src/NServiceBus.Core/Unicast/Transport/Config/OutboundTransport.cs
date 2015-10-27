namespace NServiceBus
{
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class OutboundTransport
    {
        public TransportDefinition Definition { get; }
        public bool IsDefault { get; }

        public OutboundTransport(TransportDefinition definition, bool isDefault)
        {
            Definition = definition;
            IsDefault = isDefault;
        }

        public TransportSendingConfigurationContext Configure(ReadOnlySettings settings)
        {
            var connectionString = settings.Get<TransportConnectionString>().GetConnectionStringOrRaiseError(Definition);
            var context = new TransportSendingConfigurationContext(settings, connectionString);
            Definition.ConfigureForSending(context);
            return context;
        }
    }
}