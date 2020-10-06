namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public static class UseTransportExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static void UseTransport(this EndpointConfiguration endpointConfiguration, TransportDefinition transport)
        {
            endpointConfiguration.Settings.Get<TransportSeam.Settings>().TransportDefinition = transport;
        }
    }
}