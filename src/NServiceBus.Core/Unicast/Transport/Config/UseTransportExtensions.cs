namespace NServiceBus
{
    using System;
    using NServiceBus.Utils.Reflection;
    using Transports;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public static partial class UseTransportExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions<T> UseTransport<T>(this BusConfiguration busConfiguration) where T : TransportDefinition, new()
        {
            var type = typeof(TransportExtensions<>).MakeGenericType(typeof(T));
            var extension = (TransportExtensions<T>)Activator.CreateInstance(type, busConfiguration.Settings);

            busConfiguration.Settings.Set("transportDefinitionType", typeof(T));

            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions UseTransport(this BusConfiguration busConfiguration, Type transportDefinitionType)
        {
            busConfiguration.Settings.Set("transportDefinitionType", transportDefinitionType);

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
            if (!busConfiguration.Settings.TryGet("transportDefinitionType", out transportDefinitionType))
            {
                return new MsmqTransport();
            }

            return transportDefinitionType.Construct<TransportDefinition>();
        }
    }
}
