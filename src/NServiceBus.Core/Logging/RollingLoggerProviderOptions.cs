#nullable enable

namespace NServiceBus;

using Microsoft.Extensions.Logging;

/// <summary>
/// Options for the NServiceBus rolling file logger provider.
/// </summary>
public class RollingLoggerProviderOptions
{
    /// <summary>
    /// Directory to write log files to. Defaults to null, which resolves to
    /// <c>Host.GetOutputDirectory()</c> lazily on first write inside the provider.
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>
    /// Minimum log level for the rolling file. Defaults to <see cref="LogLevel.Information"/>.
    /// </summary>
    /// <remarks>
    /// Member names differ from the legacy <c>NServiceBus.Logging.LogLevel</c>:
    /// <c>Info</c> → <c>Information</c>, <c>Warn</c> → <c>Warning</c>, <c>Fatal</c> → <c>Critical</c>.
    /// Level filtering is applied by the MEL pipeline via <c>SetMinimumLevel</c>;
    /// the provider itself only filters out <see cref="LogLevel.None"/>.
    /// </remarks>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Number of archive log files to keep. Defaults to 10.
    /// </summary>
    public int NumberOfArchiveFilesToKeep { get; set; } = 10;

    /// <summary>
    /// Maximum size of a single log file in bytes. Defaults to 10 MB.
    /// </summary>
    public long MaxFileSizeInBytes { get; set; } = 10L * 1024 * 1024;
}
