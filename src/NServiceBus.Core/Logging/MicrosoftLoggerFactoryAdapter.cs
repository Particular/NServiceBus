#pragma warning disable CA2254
#nullable enable

namespace NServiceBus;

using System;
using Microsoft.Extensions.Logging;
using MicrosoftLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using MicrosoftLogger = Microsoft.Extensions.Logging.ILogger;
using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

sealed class MicrosoftLoggerFactoryAdapter(MicrosoftLoggerFactory loggerFactory) : NServiceBus.Logging.ILoggerFactory
    , Logging.LogManager.ISlotScopedLoggerFactory
{
    readonly MicrosoftLogger scopeLogger = loggerFactory.CreateLogger("NServiceBus.Logging.Scope");

    public Logging.ILog GetLogger(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new MicrosoftLoggerAdapter(loggerFactory.CreateLogger(type.FullName!));
    }

    public Logging.ILog GetLogger(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new MicrosoftLoggerAdapter(loggerFactory.CreateLogger(name));
    }

    public IDisposable BeginScope(LogScopeState scopeState) => scopeLogger.BeginScope(scopeState) ?? NullScope.Instance;

    sealed class MicrosoftLoggerAdapter(MicrosoftLogger logger) : Logging.ILog
    {
        public bool IsDebugEnabled => logger.IsEnabled(MicrosoftLogLevel.Debug);
        public bool IsInfoEnabled => logger.IsEnabled(MicrosoftLogLevel.Information);
        public bool IsWarnEnabled => logger.IsEnabled(MicrosoftLogLevel.Warning);
        public bool IsErrorEnabled => logger.IsEnabled(MicrosoftLogLevel.Error);
        public bool IsFatalEnabled => logger.IsEnabled(MicrosoftLogLevel.Critical);

        public void Debug(string? message)
            => logger.LogDebug(message);

        public void Debug(string? message, Exception? exception)
            => logger.LogDebug(exception, message);

        public void DebugFormat(string format, params object?[] args)
            => logger.LogDebug(format, args);

        public void Info(string? message)
            => logger.LogInformation(message);

        public void Info(string? message, Exception? exception)
            => logger.LogInformation(exception, message);

        public void InfoFormat(string format, params object?[] args)
            => logger.LogInformation(format, args);

        public void Warn(string? message)
            => logger.LogWarning(message);

        public void Warn(string? message, Exception? exception)
            => logger.LogWarning(exception, message);

        public void WarnFormat(string format, params object?[] args)
            => logger.LogWarning(format, args);

        public void Error(string? message)
            => logger.LogError(message);

        public void Error(string? message, Exception? exception)
            => logger.LogError(exception, message);

        public void ErrorFormat(string format, params object?[] args)
            => logger.LogError(format, args);

        public void Fatal(string? message)
            => logger.LogCritical(message);

        public void Fatal(string? message, Exception? exception)
            => logger.LogCritical(exception, message);

        public void FatalFormat(string format, params object?[] args)
            => logger.LogCritical(format, args);
    }

    sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
#pragma warning restore CA2254
