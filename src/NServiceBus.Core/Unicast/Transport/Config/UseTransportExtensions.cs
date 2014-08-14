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
        public static void UseTransport<T>(this ConfigurationBuilder configurationBuilder, Action<TransportConfiguration> customizations = null) where T : TransportDefinition, new()
        {
            configurationBuilder.UseTransport(typeof(T), customizations);
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static void UseTransport(this ConfigurationBuilder configurationBuilder, Type transportDefinitionType, Action<TransportConfiguration> customizations = null)
        {
            configurationBuilder.settings.Set("transportDefinitionType", transportDefinitionType);
            configurationBuilder.settings.Set("transportCustomizations", customizations);
        }

        internal static void SetupTransport(ConfigurationBuilder configurationBuilder)
        {
            var transportDefinition = GetTransportDefinition(configurationBuilder);
            configurationBuilder.settings.Set<TransportDefinition>(transportDefinition);
            transportDefinition.Configure(configurationBuilder);
        }

        static TransportDefinition GetTransportDefinition(ConfigurationBuilder configurationBuilder)
        {
            Type transportDefinitionType;
            if (!configurationBuilder.settings.TryGet("transportDefinitionType", out transportDefinitionType))
            {
                return new Msmq();
            }

            var customizations = configurationBuilder.settings.Get<Action<TransportConfiguration>>("transportCustomizations");
            if (customizations != null)
            {
                customizations(new TransportConfiguration(configurationBuilder.settings));
            }

            return transportDefinitionType.Construct<TransportDefinition>();
        }
    }
}