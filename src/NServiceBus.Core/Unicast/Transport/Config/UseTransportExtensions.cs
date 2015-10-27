namespace NServiceBus
{
    using System;
    using NServiceBus.Extensibility;
    using NServiceBus.Utils.Reflection;
    using Transports;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public static class UseTransportExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions<T> UseTransport<T>(this BusConfiguration busConfiguration) where T : TransportDefinition, new()
        {
            Guard.AgainstNull(nameof(busConfiguration), busConfiguration);
            var type = typeof(TransportExtensions<>).MakeGenericType(typeof(T));
            var extension = (TransportExtensions<T>)Activator.CreateInstance(type, busConfiguration.Settings);

            var transportDefinition = new T();
            busConfiguration.Settings.Set<InboundTransport>(new InboundTransport(transportDefinition));
            busConfiguration.Settings.Set<TransportDefinition>(transportDefinition);
            AddOutboundTransport(busConfiguration, transportDefinition, true);
            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions UseTransport(this BusConfiguration busConfiguration, Type transportDefinitionType)
        {
            Guard.AgainstNull(nameof(busConfiguration), busConfiguration);
            Guard.AgainstNull(nameof(transportDefinitionType), transportDefinitionType);
            Guard.TypeHasDefaultConstructor(transportDefinitionType, nameof(transportDefinitionType));

            var transportDefinition = transportDefinitionType.Construct<TransportDefinition>();

            busConfiguration.Settings.Set<InboundTransport>(new InboundTransport(transportDefinition));
            busConfiguration.Settings.Set<TransportDefinition>(transportDefinition);
            AddOutboundTransport(busConfiguration, transportDefinition, true);
            return new TransportExtensions(busConfiguration.Settings);
        }

        static void AddOutboundTransport(BusConfiguration busConfiguration, TransportDefinition transportDefinition, bool isDefault)
        {
            busConfiguration.Settings.Set<OutboundTransport>(new OutboundTransport(transportDefinition, isDefault));
        }

        internal static void EnsureTransportConfigured(BusConfiguration busConfiguration)
        {
            if (!busConfiguration.Settings.HasExplicitValue<TransportDefinition>())
            {
                busConfiguration.UseTransport<MsmqTransport>();
            }
        }
    }
}