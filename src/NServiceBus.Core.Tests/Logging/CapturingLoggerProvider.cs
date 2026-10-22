namespace NServiceBus.Core.Tests.Logging;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Activity: Activity.Current when the entry was written, the span a log record is correlated with.
sealed record CapturedLogEntry(string Category, LogLevel Level, string Message, Exception Exception, Activity Activity);

// Captures every log entry written through Microsoft.Extensions.Logging so tests can assert on category, level,
// message and attached exception.
sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<CapturedLogEntry> Entries { get; } = new();

    public static (ServiceProvider serviceProvider, ConcurrentQueue<CapturedLogEntry> entries) CreateServiceProvider()
    {
        var provider = new CapturingLoggerProvider();
        return (new ServiceCollection().AddLogging(b => b.AddProvider(provider)).BuildServiceProvider(), provider.Entries);
    }

    public static (ILogger<T> logger, ConcurrentQueue<CapturedLogEntry> entries) CreateLogger<T>()
    {
        var (serviceProvider, entries) = CreateServiceProvider();
        return (serviceProvider.GetRequiredService<ILogger<T>>(), entries);
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

    public void Dispose() { }

    sealed class CapturingLogger(string category, ConcurrentQueue<CapturedLogEntry> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => entries.Enqueue(new CapturedLogEntry(category, logLevel, formatter(state, exception), exception, Activity.Current));
    }
}