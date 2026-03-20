#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

class CollectingLoggerProvider : ILoggerProvider
{
    public readonly List<(string Category, LogLevel Level, string Message)> LogEntries = [];

    public ILogger CreateLogger(string categoryName) => new CollectingLogger(this, categoryName);

    public void Dispose() { }

    class CollectingLogger(CollectingLoggerProvider provider, string category) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (provider.LogEntries)
            {
                provider.LogEntries.Add((category, logLevel, formatter(state, exception)));
            }
        }
    }
}