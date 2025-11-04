#nullable enable

namespace NServiceBus;

using Configuration.AdvancedExtensibility;
using Transport;

/// <summary>
/// Enables users to select the transport by calling .UseTransport().
/// </summary>
public static class TransportConfig
{
    extension(EndpointConfiguration endpointConfiguration)
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public RoutingSettings<TTransport> UseTransport<TTransport>(TTransport transportDefinition)
            where TTransport : TransportDefinition
        {
            var settings = endpointConfiguration.GetSettings();
            settings.Get<TransportSeam.Settings>().TransportDefinition = transportDefinition;
            return new RoutingSettings<TTransport>(settings);
        }
    }
}