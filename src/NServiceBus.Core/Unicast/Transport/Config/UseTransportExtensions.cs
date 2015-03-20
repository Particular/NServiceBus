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
        private const string TransportDefinitionTypeKey = "transportDefinitionType";

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions<T> UseTransport<T>(this BusConfiguration busConfiguration) where T : TransportDefinition, new()
        {
            Guard.AgainstNull(busConfiguration, "busConfiguration");
            var type = typeof(TransportExtensions<>).MakeGenericType(typeof(T));
            var extension = (TransportExtensions<T>)Activator.CreateInstance(type, busConfiguration.Settings);

            busConfiguration.Settings.Set(TransportDefinitionTypeKey, typeof(T));

            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions UseTransport(this BusConfiguration busConfiguration, Type transportDefinitionType)
        {
            Guard.AgainstNull(busConfiguration, "busConfiguration");
            Guard.AgainstNull(transportDefinitionType, "transportDefinitionType");
            Guard.TypeHasDefaultConstructor(transportDefinitionType, "transportDefinitionType");

            busConfiguration.Settings.Set(TransportDefinitionTypeKey, transportDefinitionType);

            return new TransportExtensions(busConfiguration.Settings);
        }

        internal static void SetupTransport(BusConfiguration busConfiguration)
        {
            var transportDefinition = GetTransportDefinition(busConfiguration);
            busConfiguration.Settings.Set<TransportDefinition>(transportDefinition);
            transportDefinition.Configure(busConfiguration);
        }

        static TransportDefinition GetTransportDefinition(BusConfiguration busConfiguration)
        {
            Type transportDefinitionType;
            if (!busConfiguration.Settings.TryGet(TransportDefinitionTypeKey, out transportDefinitionType))
            {
                return new MsmqTransport();
            }

            return transportDefinitionType.Construct<TransportDefinition>();
        }
    }
}