namespace NServiceBus
{
    using System;
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
            ConfigureTransport(busConfiguration, transportDefinition);
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
            ConfigureTransport(busConfiguration, transportDefinition);
            return new TransportExtensions(busConfiguration.Settings);
        }

        static void ConfigureTransport(BusConfiguration busConfiguration, TransportDefinition transportDefinition)
        {
            busConfiguration.Settings.Set<InboundTransport>(new InboundTransport(transportDefinition));
            busConfiguration.Settings.Set<TransportDefinition>(transportDefinition);
            busConfiguration.Settings.Set<OutboundTransport>(new OutboundTransport(transportDefinition, true));
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