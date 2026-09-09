#nullable enable

namespace NServiceBus;

using NServiceBus.Configuration.AdvancedExtensibility;

/// <summary>
/// Provides extension methods for configuring the service platform.
/// </summary>
public static class ServicePlatformConfigExtensions
{
    extension(EndpointConfiguration endpointConfiguration)
    {
        /// <summary>
        /// Enables the endpoint to connect to the Particular Service Platform.
        /// </summary>
        public ServicePlatformSettings EnableServicePlatform()
        {
            endpointConfiguration.EnableFeature<ServicePlatformFeature>();

            return new ServicePlatformSettings(endpointConfiguration.GetSettings());
        }
    }
}
