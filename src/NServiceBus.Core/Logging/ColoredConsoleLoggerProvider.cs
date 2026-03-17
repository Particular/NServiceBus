#nullable enable

namespace NServiceBus;

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[ProviderAlias("NServiceBusConsole")]
sealed class ColoredConsoleLoggerProvider : ILoggerProvider
{
    readonly IServiceProvider serviceProvider;
    readonly Lazy<bool> isEnabled;

    public ColoredConsoleLoggerProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        isEnabled = new Lazy<bool>(ShouldBeEnabled);
    }

    public void Dispose() { }

    public ILogger CreateLogger(string categoryName)
    {
        if (!isEnabled.Value)
        {
            return NullLogger.Instance;
        }

        return ColoredConsoleLogger.Instance;
    }

    bool ShouldBeEnabled()
    {
        var providers = serviceProvider.GetServices<ILoggerProvider>();
        return providers.All(p => p is RollingLoggerProvider or ColoredConsoleLoggerProvider);
    }

    sealed class ColoredConsoleLogger : ILogger
    {
        public static readonly ColoredConsoleLogger Instance = new();

        static ColoredConsoleLogger()
        {
            using var stream = Console.OpenStandardOutput();
            logToConsole = stream != Stream.Null;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (!logToConsole)
            {
                return;
            }

            var message = formatter(state, exception);
            try
            {
                Console.ForegroundColor = GetColor(logLevel);
                Console.WriteLine(message);
                Console.ResetColor();
            }
            catch (IOException)
            {
                logToConsole = false;
            }
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        static ConsoleColor GetColor(LogLevel logLevel)
        {
            if (logLevel >= LogLevel.Error)
            {
                return ConsoleColor.Red;
            }

            if (logLevel == LogLevel.Warning)
            {
                return ConsoleColor.DarkYellow;
            }

            return ConsoleColor.White;
        }

        static bool logToConsole;

        sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}