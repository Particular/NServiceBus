namespace NServiceBus;

using System;
using Features;

/// <summary>
/// Contains extension methods to configure license.
/// </summary>
public static class ConfigureLicenseExtensions
{
    /// <summary>
    /// Allows user to specify the license string.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    /// <param name="licenseText">The license text.</param>
    /// <remarks>The license provided via code-first API takes precedence over other license sources.</remarks>
    public static void License(this EndpointConfiguration config, string licenseText)
    {
        // Intentionally not doing validation as passed value could be dynamic and (temporarily) be invalid
        config.Settings.Set(LicenseReminder.LicenseTextSettingsKey, licenseText);
    }

    /// <summary>
    /// Allows user to specify the path for the license file.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    /// <param name="licenseFile">A relative or absolute path to the license file.</param>
    /// <remarks>The license provided via code-first API takes precedence over other license sources.</remarks>
    public static void LicensePath(this EndpointConfiguration config, string licenseFile)
    {
        // Intentionally not doing validation as passed value could be dynamic and (temporarily) be invalid
        config.Settings.Set(LicenseReminder.LicenseFilePathSettingsKey, licenseFile);
    }
}