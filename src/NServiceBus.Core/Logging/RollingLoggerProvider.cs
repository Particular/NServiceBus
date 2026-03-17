#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

[ProviderAlias("NServiceBusRollingFile")]
sealed class RollingLoggerProvider(
    string loggingDirectory,
    int numberOfArchiveFilesToKeep = 10,
    long maxFileSize = 10L * 1024 * 1024) : ILoggerProvider, ISupportExternalScope
{
    readonly ConcurrentDictionary<string, RollingMicrosoftLogger> loggers = new();
    readonly RollingLogger rollingLogger = new(loggingDirectory, numberOfArchiveFilesToKeep, maxFileSize);
    readonly Lock locker = new();
    IExternalScopeProvider scopeProvider = new LoggerExternalScopeProvider();

    public void Dispose() => loggers.Clear();

    public ILogger CreateLogger(string categoryName) =>
        loggers.GetOrAdd(categoryName, static (_, provider) => new RollingMicrosoftLogger(provider), this);

    public void SetScopeProvider(IExternalScopeProvider externalScopeProvider) => scopeProvider = externalScopeProvider;

    void Write(LogLevel logLevel, string? message, Exception? exception)
    {
        var stringBuilder = new StringBuilder();
#pragma warning disable PS0023 // Logging should use local time because logging with UTC can cause confusion and the assumption is the server runs in a timezone that makes sense (most likely UTC)
        var datePart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
#pragma warning restore PS0023
        var paddedLevel = LogLevelName(logLevel);

        stringBuilder.Append(datePart).Append(' ').Append(paddedLevel).Append(' ').Append(message);

        if (exception != null)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append(exception);
            if (exception.Data.Count > 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("Exception details:");

                foreach (DictionaryEntry exceptionData in exception.Data)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append('\t').Append(exceptionData.Key).Append(": ").Append(exceptionData.Value);
                }
            }
        }

        var fullMessage = stringBuilder.ToString();
        lock (locker)
        {
            rollingLogger.WriteLine(fullMessage);
        }
    }

    static string LogLevelName(LogLevel level) =>
        level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "FATAL",
            LogLevel.Trace => "TRACE",
            LogLevel.None => "NONE ",
            _ => level.ToString().ToUpper().PadRight(5)
        };

    sealed class RollingMicrosoftLogger(RollingLoggerProvider provider) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            provider.Write(logLevel, message, exception);
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            provider.scopeProvider.Push(state);
    }
}