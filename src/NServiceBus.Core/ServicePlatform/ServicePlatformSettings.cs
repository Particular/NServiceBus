#nullable enable

namespace NServiceBus;

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
    /// Gets or sets the primary ServiceControl queue.
    /// </summary>
    public string ServiceControlQueue
    {
        get => Settings.Get<string>(ServicePlatform.ServiceControlQueueSettingKey);
        set => Settings.Set(ServicePlatform.ServiceControlQueueSettingKey, value);
    }
}
