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
        public static TransportExtentions<T> UseTransport<T>(this BusConfiguration busConfiguration) where T : TransportDefinition, new()
        {
            var type = typeof(TransportExtentions<>).MakeGenericType(typeof(T));
            var extension = (TransportExtentions<T>)Activator.CreateInstance(type, busConfiguration.Settings);

            busConfiguration.UseTransport(typeof(T));

            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtentions UseTransport(this BusConfiguration busConfiguration, Type transportDefinitionType)
        {
            busConfiguration.Settings.Set("transportDefinitionType", transportDefinitionType);

            return new TransportExtentions(busConfiguration.Settings);
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
                return new Msmq();
            }

            return transportDefinitionType.Construct<TransportDefinition>();
        }
    }
}
