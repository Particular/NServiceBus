#nullable enable

namespace NServiceBus.Logging;

using System;
using Particular.Obsoletes;

/// <summary>
/// Base class for logging definitions.
/// </summary>
[ObsoleteMetadata(
    Message = "Implement Microsoft.Extensions.Logging.ILoggerProvider directly and register via services.AddSingleton<ILoggerProvider, YourProvider>()",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12")]
[Obsolete("Implement Microsoft.Extensions.Logging.ILoggerProvider directly and register via services.AddSingleton<ILoggerProvider, YourProvider>(). Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public abstract class LoggingFactoryDefinition
{
    /// <summary>
    /// Constructs an instance of <see cref="ILoggerFactory" /> for use by <see cref="LogManager.Use{T}" />.
    /// </summary>
    protected internal abstract ILoggerFactory GetLoggingFactory();
}
