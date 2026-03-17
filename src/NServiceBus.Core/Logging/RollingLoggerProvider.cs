#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[ProviderAlias("NServiceBusRollingFile")]
sealed class RollingLoggerProvider : ILoggerProvider
{
    readonly IServiceProvider serviceProvider;
    readonly string loggingDirectory;
    readonly int numberOfArchiveFilesToKeep;
    readonly long maxFileSize;
    readonly ConcurrentDictionary<string, ILogger> loggers = new();
    readonly Lazy<bool> isEnabled;
    RollingLogger? rollingLogger;
    readonly Lock locker = new();

    public RollingLoggerProvider(
        IServiceProvider serviceProvider,
        string loggingDirectory,
        int numberOfArchiveFilesToKeep = 10,
        long maxFileSize = 10L * 1024 * 1024)
    {
        this.serviceProvider = serviceProvider;
        this.loggingDirectory = loggingDirectory;
        this.numberOfArchiveFilesToKeep = numberOfArchiveFilesToKeep;
        this.maxFileSize = maxFileSize;
        isEnabled = new Lazy<bool>(ShouldBeEnabled);
    }

    public void Dispose() => loggers.Clear();

    public ILogger CreateLogger(string categoryName)
    {
        if (!isEnabled.Value)
        {
            return NullLogger.Instance;
        }

        lock (locker)
        {
            rollingLogger ??= new RollingLogger(loggingDirectory, numberOfArchiveFilesToKeep, maxFileSize);
        }
        return loggers.GetOrAdd(categoryName, static (_, provider) => new RollingMicrosoftLogger(provider), this);
    }

    bool ShouldBeEnabled()
    {
        var providers = serviceProvider.GetServices<ILoggerProvider>();
        return providers.All(p => p is RollingLoggerProvider or ColoredConsoleLoggerProvider);
    }

    void Write(LogLevel logLevel, string? message, Exception? exception)
    {
        if (!isEnabled.Value || rollingLogger == null)
        {
            return;
        }

        var stringBuilder = new StringBuilder();
#pragma warning disable PS0023
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

                foreach (System.Collections.DictionaryEntry exceptionData in exception.Data)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append('\t').Append(exceptionData.Key).Append(": ").Append(exceptionData.Value);
                }
            }
        }

        lock (locker)
        {
            rollingLogger.WriteLine(stringBuilder.ToString());
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

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && provider.isEnabled.Value;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}