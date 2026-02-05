#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Configuration.AdvancedExtensibility;
using Settings;

/// <summary>
/// Provides an API to add startup diagnostics.
/// </summary>
public static class DiagnosticSettingsExtensions
{
    /// <summary>
    /// Adds a section to the startup diagnostics.
    /// </summary>
    public static void AddStartupDiagnosticsSection(this IReadOnlySettings settings, string sectionName, object section)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        ArgumentNullException.ThrowIfNull(section);

        settings.Get<HostingComponent.Settings>().StartupDiagnostics.Add(sectionName, section);
    }

    /// <summary>
    /// Configures a custom path where host diagnostics is written.
    /// </summary>
    /// <param name="config">Configuration object to extend.</param>
    /// <param name="path">The custom path to use.</param>
    [NotSupportedInEnvironment(EnvironmentIds.Serverless, "Diagnostics path is managed by the runtime.")]
    public static void SetDiagnosticsPath(this EndpointConfiguration config, string path)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        PathChecker.ThrowForBadPath(path, "Diagnostics root path");

        config.GetSettings().Get<HostingComponent.Settings>().DiagnosticsPath = path;
    }

    /// <summary>
    /// Allows full control over how diagnostics data is persisted.
    /// </summary>
    /// <param name="config">Configuration object to extend.</param>
    /// <param name="customDiagnosticsWriter">Func responsible for writing diagnostics data.</param>
    public static void CustomDiagnosticsWriter(this EndpointConfiguration config, Func<string, CancellationToken, Task> customDiagnosticsWriter)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(customDiagnosticsWriter);

        config.Settings.Get<HostingComponent.Settings>().HostDiagnosticsWriter = customDiagnosticsWriter;
    }
}