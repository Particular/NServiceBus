#nullable enable

namespace NServiceBus;

using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Configuration.AdvancedExtensibility;
using Particular.Obsoletes;
using Settings;

/// <summary>
/// Provides an API to add startup diagnostics.
/// </summary>
public static class DiagnosticSettingsExtensions
{
    /// <summary>
    /// Adds a section to the startup diagnostics.
    /// </summary>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7883",
        ReplacementTypeOrMember = "AddStartupDiagnosticsSection<T>(IReadOnlySettings, string, T, JsonTypeInfo<T>)",
        Note = "The non-generic overload uses reflection-based serialization which is not AOT/trimming safe. Use the generic overload with a JsonTypeInfo<T> instead.")]
    public static void AddStartupDiagnosticsSection(this IReadOnlySettings settings, string sectionName, object section)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        ArgumentNullException.ThrowIfNull(section);

        settings.Get<HostingComponent.Settings>().StartupDiagnostics.Add(sectionName, section);
    }

    /// <summary>
    /// Adds a section to the startup diagnostics with a strongly-typed value and its JSON type info for AOT-safe serialization.
    /// </summary>
    /// <remarks>
    /// Use this overload when the diagnostics value is cheap to compute.
    /// For expensive operations that should only run when diagnostics are actually written, use
    /// <see cref="AddStartupDiagnosticsSectionFactory{T}(IReadOnlySettings, string, Func{T}, JsonTypeInfo{T})"/> instead.
    /// </remarks>
    public static void AddStartupDiagnosticsSection<T>(this IReadOnlySettings settings, string sectionName, T section, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(typeInfo);

        settings.Get<HostingComponent.Settings>().StartupDiagnostics.Add(sectionName, section, typeInfo);
    }

    /// <summary>
    /// Adds a section to the startup diagnostics with a factory that is evaluated lazily and its JSON type info for AOT-safe serialization.
    /// </summary>
    /// <remarks>
    /// Use this overload when the diagnostics value is expensive to compute and should only be evaluated
    /// when diagnostics are actually written. For cheap values, prefer
    /// <see cref="AddStartupDiagnosticsSection{T}(IReadOnlySettings, string, T, JsonTypeInfo{T})"/> instead.
    /// </remarks>
    public static void AddStartupDiagnosticsSectionFactory<T>(this IReadOnlySettings settings, string sectionName, Func<T> sectionFactory, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        ArgumentNullException.ThrowIfNull(sectionFactory);
        ArgumentNullException.ThrowIfNull(typeInfo);

        settings.Get<HostingComponent.Settings>().StartupDiagnostics.AddFactory(sectionName, sectionFactory, typeInfo);
    }

    /// <summary>
    /// Configures a custom path where host diagnostics is written.
    /// </summary>
    /// <param name="config">Configuration object to extend.</param>
    /// <param name="path">The custom path to use.</param>
    public static void SetDiagnosticsPath(this EndpointConfiguration config, string path)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        PathChecker.ThrowForBadPath(path, "Diagnostics root path");

        config.GetSettings().Get<HostingComponent.Settings>().DiagnosticsPath = path;
    }

    /// <summary>
    /// Writes diagnostics to log in addition to the file or the custom diagnostic writer.
    /// </summary>
    /// <param name="config">Configuration object to extend.</param>
    public static void WriteDiagnosticsToLog(this EndpointConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.GetSettings().Get<HostingComponent.Settings>().WriteDiagnosticsToLog = true;
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