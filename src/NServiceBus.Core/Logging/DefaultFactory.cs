#nullable enable

namespace NServiceBus.Logging;

using System;
using System.IO;
using Particular.Obsoletes;
using IODirectory = System.IO.Directory;

/// <summary>
/// The default <see cref="LoggingFactoryDefinition" />.
/// </summary>
[ObsoleteMetadata(
    Message = "Use services.Configure<RollingLoggerProviderOptions>() to configure the built-in rolling file logging provider",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12")]
[Obsolete("Use services.Configure<RollingLoggerProviderOptions>() to configure the built-in rolling file logging provider. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public class DefaultFactory : LoggingFactoryDefinition
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultFactory" />.
    /// </summary>
    public DefaultFactory()
    {
        directory = new Lazy<string>(() =>
        {
            try
            {
                return Host.GetOutputDirectory();
            }
            catch (Exception e)
            {
                throw new Exception("Unable to determine the logging output directory. Check the inner exception for further information, or configure a custom logging directory using 'services.Configure<RollingLoggerProviderOptions>(o => o.Directory = \"...\")'.", e);
            }
        });
        level = new Lazy<LogLevel>(() => LogLevel.Info);
    }

    internal string LoggingDirectory => directory.Value;

    internal LogLevel LoggingLevel => level.Value;

    /// <summary>
    /// <see cref="LoggingFactoryDefinition.GetLoggingFactory" />.
    /// </summary>
    protected internal override ILoggerFactory GetLoggingFactory()
    {
        var loggerFactory = new DefaultLoggerFactory(level.Value, directory.Value);
        var message = $"Logging to '{directory}' with level {level}";
        loggerFactory.Write(GetType().Name, LogLevel.Info, message);

        return loggerFactory;
    }

    /// <summary>
    /// Controls the <see cref="LogLevel" />.
    /// </summary>
    [ObsoleteMetadata(
        Message = "Set RollingLoggerProviderOptions.LogLevel instead. Note: NServiceBus.Logging.LogLevel.Info maps to Microsoft.Extensions.Logging.LogLevel.Information, Warn to Warning, Fatal to Critical",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12")]
    [Obsolete("Set RollingLoggerProviderOptions.LogLevel instead. Note: NServiceBus.Logging.LogLevel.Info maps to Microsoft.Extensions.Logging.LogLevel.Information, Warn to Warning, Fatal to Critical. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public void Level(LogLevel level) => this.level = new Lazy<LogLevel>(() => level);

    /// <summary>
    /// The directory to log files to.
    /// </summary>
    [ObsoleteMetadata(
        Message = "Set RollingLoggerProviderOptions.Directory instead",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12")]
    [Obsolete("Set RollingLoggerProviderOptions.Directory instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public void Directory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!IODirectory.Exists(directory))
        {
            var message = $"Could not find logging directory: '{directory}'";
            throw new DirectoryNotFoundException(message);
        }

        this.directory = new Lazy<string>(() => directory);
    }

    Lazy<string> directory;
    Lazy<LogLevel> level;
}
