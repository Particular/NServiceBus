#pragma warning disable CA2254
#nullable enable

namespace NServiceBus.Logging;

using System;
using Microsoft.Extensions.Logging;
using MicrosoftLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using MicrosoftLogger = Microsoft.Extensions.Logging.ILogger;
using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

sealed class MicrosoftLoggerFactoryAdapter(MicrosoftLoggerFactory loggerFactory) : ILoggerFactory
{
    public ILog GetLogger(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new MicrosoftLoggerAdapter(loggerFactory.CreateLogger(type.FullName!));
    }

    public ILog GetLogger(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new MicrosoftLoggerAdapter(loggerFactory.CreateLogger(name));
    }

    sealed class MicrosoftLoggerAdapter(MicrosoftLogger logger) : ILog
    {
        public bool IsDebugEnabled => logger.IsEnabled(MicrosoftLogLevel.Debug);
        public bool IsInfoEnabled => logger.IsEnabled(MicrosoftLogLevel.Information);
        public bool IsWarnEnabled => logger.IsEnabled(MicrosoftLogLevel.Warning);
        public bool IsErrorEnabled => logger.IsEnabled(MicrosoftLogLevel.Error);
        public bool IsFatalEnabled => logger.IsEnabled(MicrosoftLogLevel.Critical);

        public void Debug(string? message)
        {
            using var _ = BeginScope();
            logger.LogDebug(message);
        }

        public void Debug(string? message, Exception? exception)
        {
            using var _ = BeginScope();
            logger.LogDebug(exception, message);
        }

        public void DebugFormat(string format, params object?[] args)
        {
            using var _ = BeginScope();
            logger.LogDebug(format, args);
        }

        public void Info(string? message)
        {
            using var _ = BeginScope();
            logger.LogInformation(message);
        }

        public void Info(string? message, Exception? exception)
        {
            using var _ = BeginScope();
            logger.LogInformation(exception, message);
        }

        public void InfoFormat(string format, params object?[] args)
        {
            using var _ = BeginScope();
            logger.LogInformation(format, args);
        }

        public void Warn(string? message)
        {
            using var _ = BeginScope();
            logger.LogWarning(message);
        }

        public void Warn(string? message, Exception? exception)
        {
            using var _ = BeginScope();
            logger.LogWarning(exception, message);
        }

        public void WarnFormat(string format, params object?[] args)
        {
            using var _ = BeginScope();
            logger.LogWarning(format, args);
        }

        public void Error(string? message)
        {
            using var _ = BeginScope();
            logger.LogError(message);
        }

        public void Error(string? message, Exception? exception)
        {
            using var _ = BeginScope();
            logger.LogError(exception, message);
        }

        public void ErrorFormat(string format, params object?[] args)
        {
            using var _ = BeginScope();
            logger.LogError(format, args);
        }

        public void Fatal(string? message)
        {
            using var _ = BeginScope();
            logger.LogCritical(message);
        }

        public void Fatal(string? message, Exception? exception)
        {
            using var _ = BeginScope();
            logger.LogCritical(exception, message);
        }

        public void FatalFormat(string format, params object?[] args)
        {
            using var _ = BeginScope();
            logger.LogCritical(format, args);
        }

        IDisposable BeginScope()
        {
            if (!LogManager.TryGetCurrentEndpointScopeState(out var scopeState))
            {
                return NullScope.Instance;
            }

            return logger.BeginScope(scopeState) ?? NullScope.Instance;
        }

        sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }

    }
}
#pragma warning restore CA2254