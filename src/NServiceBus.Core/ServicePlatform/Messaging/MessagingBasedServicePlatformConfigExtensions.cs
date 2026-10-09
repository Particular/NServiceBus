#nullable enable

namespace NServiceBus;

using System;
using NServiceBus.Configuration.AdvancedExtensibility;

/// <summary>
/// Configuration options for messaging based Service Platform connection.
/// </summary>
public static class MessagingBasedServicePlatformConfigExtensions
{
    extension(EndpointConfiguration endpointConfiguration)
    {
        /// <summary>
        /// Set the primary instance queue of the ServiceControl Error instance for this endpoint.
        /// </summary>
        public void ServiceControlErrorInstanceQueue(string queue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queue);
            endpointConfiguration.GetSettings().Set(MessagingBasedServicePlatformConnection.ServiceControlQueueSettingKey, queue);
        }
    }
}
