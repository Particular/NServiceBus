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
            var contextBag = new ContextBag();
            var extension = (TransportExtensions<T>)Activator.CreateInstance(type, contextBag);

            var transportDefinition = new T();
            busConfiguration.Settings.Set<InboundTransport>(new InboundTransport(transportDefinition, contextBag));
            busConfiguration.Settings.Set<TransportDefinition>(transportDefinition);
            AddOutboundTransport(busConfiguration, transportDefinition, contextBag, true);
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

            var contextBag = new ContextBag();
            var transportDefinition = transportDefinitionType.Construct<TransportDefinition>();

            busConfiguration.Settings.Set<InboundTransport>(new InboundTransport(transportDefinition, contextBag));
            busConfiguration.Settings.Set<TransportDefinition>(transportDefinition);
            AddOutboundTransport(busConfiguration, transportDefinition, contextBag, true);
            return new TransportExtensions(contextBag);
        }
        
        static void AddOutboundTransport(BusConfiguration busConfiguration, TransportDefinition transportDefinition, ContextBag contextBag, bool isDefault)
        {
            OutboundTransports outboundTransports;
            if (!busConfiguration.Settings.TryGet(out outboundTransports))
            {
                outboundTransports = new OutboundTransports();
                busConfiguration.Settings.Set<OutboundTransports>(outboundTransports);
            }
            outboundTransports.Add(transportDefinition, contextBag, isDefault);
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

namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public static class UseTransportExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport for sending messages out.
        /// </summary>
        public static TransportExtensions<T> UseAdditionalOutgoingTransport<T>(this BusConfiguration busConfiguration) where T : TransportDefinition, new()
        {
            Guard.AgainstNull(nameof(busConfiguration), busConfiguration);
            var type = typeof(TransportExtensions<>).MakeGenericType(typeof(T));
            var contextBag = new ContextBag();
            var extension = (TransportExtensions<T>)Activator.CreateInstance(type, contextBag);

            var transportDefinition = new T();
            AddOutboundTransport(busConfiguration, transportDefinition, contextBag, false);
            return extension;
        }

        static void AddOutboundTransport(BusConfiguration busConfiguration, TransportDefinition transportDefinition, ContextBag contextBag, bool isDefault)
        {
            OutboundTransports outboundTransports;
            if (!busConfiguration.Settings.TryGet(out outboundTransports))
            {
                outboundTransports = new OutboundTransports();
                busConfiguration.Settings.Set<OutboundTransports>(outboundTransports);
            }
            outboundTransports.Add(transportDefinition, contextBag, isDefault);
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