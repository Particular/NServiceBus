#nullable enable

namespace NServiceBus;

using System;
using Installation;

/// <summary>
/// Convenience methods for configuring how manifest information is generated.
/// </summary>
public static class ManifestConfigExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    /// <param name="outputPath">The outputPath to  <see cref="INeedToInstallSomething.Install" />.</param>
    public static void EnableManifestGeneration(this EndpointConfiguration config, string? outputPath)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (outputPath != null)
        {
            config.Settings.Set("Manifest.Path", outputPath);
        }

        config.Settings.Set("Manifest.Enable", true);
    }
}