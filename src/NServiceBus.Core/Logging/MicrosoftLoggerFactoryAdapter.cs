#nullable enable

namespace NServiceBus.Logging;

using System;
using System.Collections;
using System.Collections.Generic;
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

        public void Debug(string? message) => Log(MicrosoftLogLevel.Debug, message);
        public void Debug(string? message, Exception? exception) => Log(MicrosoftLogLevel.Debug, message, exception);
        public void DebugFormat(string format, params object?[] args) => Log(MicrosoftLogLevel.Debug, string.Format(format, args));
        public void Info(string? message) => Log(MicrosoftLogLevel.Information, message);
        public void Info(string? message, Exception? exception) => Log(MicrosoftLogLevel.Information, message, exception);
        public void InfoFormat(string format, params object?[] args) => Log(MicrosoftLogLevel.Information, string.Format(format, args));
        public void Warn(string? message) => Log(MicrosoftLogLevel.Warning, message);
        public void Warn(string? message, Exception? exception) => Log(MicrosoftLogLevel.Warning, message, exception);
        public void WarnFormat(string format, params object?[] args) => Log(MicrosoftLogLevel.Warning, string.Format(format, args));
        public void Error(string? message) => Log(MicrosoftLogLevel.Error, message);
        public void Error(string? message, Exception? exception) => Log(MicrosoftLogLevel.Error, message, exception);
        public void ErrorFormat(string format, params object?[] args) => Log(MicrosoftLogLevel.Error, string.Format(format, args));
        public void Fatal(string? message) => Log(MicrosoftLogLevel.Critical, message);
        public void Fatal(string? message, Exception? exception) => Log(MicrosoftLogLevel.Critical, message, exception);
        public void FatalFormat(string format, params object?[] args) => Log(MicrosoftLogLevel.Critical, string.Format(format, args));

        void Log(MicrosoftLogLevel level, string? message, Exception? exception = null)
        {
            using var _ = BeginScope();
            logger.Log(level, eventId: default, state: message, exception, static (s, _) => s ?? string.Empty);
        }

        IDisposable BeginScope()
        {
            if (!LogManager.TryGetCurrentEndpointIdentifier(out var endpointIdentifier))
            {
                return NullScope.Instance;
            }

            return logger.BeginScope(new EndpointScope(endpointIdentifier)) ?? NullScope.Instance;
        }

        sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }

        sealed class EndpointScope(object endpointIdentifier) : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public KeyValuePair<string, object?> this[int index] =>
                index switch
                {
                    0 => new KeyValuePair<string, object?>("Endpoint", endpointIdentifier),
                    _ => throw new ArgumentOutOfRangeException(nameof(index))
                };

            public int Count => 1;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                yield return this[0];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public override string ToString() => $"Endpoint = {endpointIdentifier}";
        }
    }
}