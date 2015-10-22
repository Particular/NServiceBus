namespace NServiceBus
{
    using NServiceBus.Extensibility;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class InboundTransport
    {
        ContextBag extensions;

        public TransportDefinition Definition { get; }

        public InboundTransport(TransportDefinition transportDefinition, ContextBag extensions)
        {
            Definition = transportDefinition;
            this.extensions = extensions;
        }

        public TransportReceivingConfigurationContext Configure(ReadOnlySettings settings)
        {
            var connectionString = extensions.Get<TransportConnectionString>().GetConnectionStringOrRaiseError(Definition);
            var context = new TransportReceivingConfigurationContext(extensions, settings, connectionString);
            Definition.ConfigureForReceiving(context);
            return context;
        }        
    }
}