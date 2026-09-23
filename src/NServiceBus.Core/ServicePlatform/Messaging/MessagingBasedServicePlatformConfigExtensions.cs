#nullable enable

namespace NServiceBus;

using System;
using NServiceBus.Configuration.AdvancedExtensibility;

/// <summary>
/// Configuration options for messaging based Service Platform connection.
/// </summary>
public static class MessagingBasedServicePlatformConfigExtensions
{
    extension(ServicePlatformSettings settings)
    {
        /// <summary>
        /// Sets the primary ServiceControl input queue.
        /// </summary>
        public ServicePlatformSettings PrimaryInstanceQueue(string queue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queue);
            settings.GetSettings().Set(MessagingBasedServicePlatformConnection.ServiceControlQueueSettingKey, queue);
            return settings;

        }
    }
}
