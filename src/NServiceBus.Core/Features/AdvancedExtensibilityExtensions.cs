namespace NServiceBus.Configuration.AdvancedExtensibility;

using System;
using Settings;

/// <summary>
/// Extension methods declarations.
/// </summary>
public static class AdvancedExtensibilityExtensions
{
    /// <summary>
    /// Gives access to the <see cref="SettingsHolder" /> for extensibility.
    /// </summary>
    public static SettingsHolder GetSettings(this ExposeSettings config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.Settings;
    }
}