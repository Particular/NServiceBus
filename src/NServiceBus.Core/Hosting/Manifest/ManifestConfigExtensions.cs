#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Enables generation of manifest data and provides full control over how manifest data is persisted.
    /// </summary>
    /// <param name="config">Configuration object to extend.</param>
    /// <param name="endpointManifestWriter">Func responsible for writing maifest data.</param>
    public static void EnableManifestGeneration(this EndpointConfiguration config, Func<string, CancellationToken, Task> endpointManifestWriter)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(endpointManifestWriter);

        config.Settings.Get<HostingComponent.Settings>().EndpointManifestWriter = endpointManifestWriter;
        config.Settings.Set("Manifest.Enable", true);
    }
}