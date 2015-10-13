namespace NServiceBus
{
    using NServiceBus.Extensibility;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class OutboundTransport
    {
        ContextBag extensions;

        public TransportDefinition Definition { get; }
        public bool IsDefault { get; }

        public OutboundTransport(TransportDefinition definition, ContextBag extensions, bool isDefault)
        {
            Definition = definition;
            this.extensions = extensions;
            IsDefault = isDefault;
        }

        public TransportSendingConfigurationContext Configure(ReadOnlySettings settings)
        {
            var connectionString = extensions.Get<TransportConnectionString>().GetConnectionStringOrRaiseError(Definition);
            var context = new TransportSendingConfigurationContext(extensions, settings, connectionString);
            Definition.ConfigureForSending(context);
            return context;
        }
    }
}