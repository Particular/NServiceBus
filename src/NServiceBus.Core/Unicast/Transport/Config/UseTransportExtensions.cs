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
        public static TransportExtentions<T> UseTransport<T>(this ConfigurationBuilder configurationBuilder) where T : TransportDefinition, new()
        {
            var type = typeof(TransportExtentions<>).MakeGenericType(typeof(T));
            var extension = (TransportExtentions<T>)Activator.CreateInstance(type, configurationBuilder.Settings);

            configurationBuilder.UseTransport(typeof(T));

            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtentions UseTransport(this ConfigurationBuilder configurationBuilder, Type transportDefinitionType)
        {
            configurationBuilder.Settings.Set("transportDefinitionType", transportDefinitionType);

            return new TransportExtentions(configurationBuilder.Settings);
        }

        internal static void SetupTransport(ConfigurationBuilder configurationBuilder)
        {
            var transportDefinition = GetTransportDefinition(configurationBuilder);
            configurationBuilder.Settings.Set<TransportDefinition>(transportDefinition);
            transportDefinition.Configure(configurationBuilder);
        }

        static TransportDefinition GetTransportDefinition(ConfigurationBuilder configurationBuilder)
        {
            Type transportDefinitionType;
            if (!configurationBuilder.Settings.TryGet("transportDefinitionType", out transportDefinitionType))
            {
                return new Msmq();
            }

            return transportDefinitionType.Construct<TransportDefinition>();
        }
    }
}
