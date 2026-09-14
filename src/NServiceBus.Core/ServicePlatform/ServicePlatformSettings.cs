#nullable enable

namespace NServiceBus;

using System;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Settings;

/// <summary>
/// Exposes the service platform configuration settings.
/// </summary>
public class ServicePlatformSettings : ExposeSettings
{
    internal ServicePlatformSettings(SettingsHolder settings) : base(settings)
    {
    }

    /// <summary>
    /// Sets the primary ServiceControl input queue.
    /// </summary>
    public ServicePlatformSettings PrimaryInstanceQueue(string queue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        Settings.Set(ServicePlatform.ServiceControlQueueSettingKey, queue);
        return this;
    }
}
