#nullable enable

namespace NServiceBus;

using System;
using Logging;
using MicrosoftLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using MicrosoftLogger = Microsoft.Extensions.Logging.ILogger;
using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

sealed class MicrosoftLoggerFactoryAdapter(MicrosoftLoggerFactory loggerFactory) : ILoggerFactory
    , LogManager.ISlotScopedLoggerFactory
{
    readonly MicrosoftLogger scopeLogger = loggerFactory.CreateLogger("NServiceBus.Logging.Scope");

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

    public IDisposable BeginScope(LogScopeState scopeState) => scopeLogger.BeginScope(scopeState) ?? NullScope.Instance;

    sealed class MicrosoftLoggerAdapter(MicrosoftLogger logger) : ILog
    {
        static readonly Func<string?, Exception?, string> MessageFormatter = static (state, _) => state ?? string.Empty;
        static readonly Func<(string format, object?[] args), Exception?, string> FormatMessageFormatter = static (state, _) => string.Format(state.format, state.args);

        public bool IsDebugEnabled => logger.IsEnabled(MicrosoftLogLevel.Debug);
        public bool IsInfoEnabled => logger.IsEnabled(MicrosoftLogLevel.Information);
        public bool IsWarnEnabled => logger.IsEnabled(MicrosoftLogLevel.Warning);
        public bool IsErrorEnabled => logger.IsEnabled(MicrosoftLogLevel.Error);
        public bool IsFatalEnabled => logger.IsEnabled(MicrosoftLogLevel.Critical);

        public void Debug(string? message) => Write(MicrosoftLogLevel.Debug, message);

        public void Debug(string? message, Exception? exception) => Write(MicrosoftLogLevel.Debug, message, exception);

        public void DebugFormat(string format, params object?[] args) => WriteFormat(MicrosoftLogLevel.Debug, format, args);

        public void Info(string? message) => Write(MicrosoftLogLevel.Information, message);

        public void Info(string? message, Exception? exception) => Write(MicrosoftLogLevel.Information, message, exception);

        public void InfoFormat(string format, params object?[] args) => WriteFormat(MicrosoftLogLevel.Information, format, args);

        public void Warn(string? message) => Write(MicrosoftLogLevel.Warning, message);

        public void Warn(string? message, Exception? exception) => Write(MicrosoftLogLevel.Warning, message, exception);

        public void WarnFormat(string format, params object?[] args) => WriteFormat(MicrosoftLogLevel.Warning, format, args);

        public void Error(string? message) => Write(MicrosoftLogLevel.Error, message);

        public void Error(string? message, Exception? exception) => Write(MicrosoftLogLevel.Error, message, exception);

        public void ErrorFormat(string format, params object?[] args) => WriteFormat(MicrosoftLogLevel.Error, format, args);

        public void Fatal(string? message) => Write(MicrosoftLogLevel.Critical, message);

        public void Fatal(string? message, Exception? exception) => Write(MicrosoftLogLevel.Critical, message, exception);

        public void FatalFormat(string format, params object?[] args) => WriteFormat(MicrosoftLogLevel.Critical, format, args);

        void Write(MicrosoftLogLevel level, string? message, Exception? exception = null)
        {
            if (!logger.IsEnabled(level))
            {
                return;
            }

            logger.Log(level, default, message, exception, MessageFormatter);
        }

        void WriteFormat(MicrosoftLogLevel level, string format, object?[] args)
        {
            if (!logger.IsEnabled(level))
            {
                return;
            }

            logger.Log(level, default, (format, args), null, FormatMessageFormatter);
        }
    }

    sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}