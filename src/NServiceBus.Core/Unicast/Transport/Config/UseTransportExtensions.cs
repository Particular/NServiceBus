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
            configurationBuilder.Settings.Set("transportDefinitionType", transportDefinitionType);
            configurationBuilder.Settings.Set("transportCustomizations", customizations);
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

            var customizations = configurationBuilder.Settings.Get<Action<TransportConfiguration>>("transportCustomizations");
            if (customizations != null)
            {
                customizations(new TransportConfiguration(configurationBuilder.Settings));
            }

            return transportDefinitionType.Construct<TransportDefinition>();
        }
    }
}
