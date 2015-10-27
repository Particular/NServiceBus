namespace NServiceBus
{
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class InboundTransport
    {
        public TransportDefinition Definition { get; }

        public InboundTransport(TransportDefinition transportDefinition)
        {
            Definition = transportDefinition;
        }

        public TransportReceivingConfigurationContext Configure(ReadOnlySettings settings)
        {
            var connectionString = settings.Get<TransportConnectionString>().GetConnectionStringOrRaiseError(Definition);
            var context = new TransportReceivingConfigurationContext(settings, connectionString);
            Definition.ConfigureForReceiving(context);
            return context;
        }        
    }
}