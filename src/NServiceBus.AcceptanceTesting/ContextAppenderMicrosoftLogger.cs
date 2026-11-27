namespace NServiceBus.AcceptanceTesting;

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

class ContextAppenderMicrosoftLogger(string name, ScenarioContext scenarioContext, IExternalScopeProvider externalScopeProvider) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel, out var messageSeverity))
        {
            return;
        }

        var context = ScenarioContext.Current ?? scenarioContext;

        string message = exception is null ? formatter(state, exception) : $"{formatter(state, exception)} {exception}";
        if (context.IncludeLoggingScopes)
        {
            var stringBuilder = new StringBuilder();
            _ = stringBuilder.Append(message);
            externalScopeProvider.ForEachScope(static (scope, builder) =>
            {
                if (scope is null)
                {
                    return;
                }
                builder.Append($"({scope.GetType().Name}: {scope})");
            }, stringBuilder);

            message = stringBuilder.ToString();
        }

        context.Logs.Enqueue(new ScenarioContext.LogItem
        {
            Endpoint = ScenarioContext.CurrentEndpoint,
            LoggerName = name,
            Level = messageSeverity,
            Message = message,
            Exception = exception,
            TraceId = Activity.Current?.TraceId,
            SpanId = Activity.Current?.SpanId
        });
    }

    public bool IsEnabled(LogLevel logLevel) => IsEnabled(logLevel, out _);

    bool IsEnabled(LogLevel logLevel, out Logging.LogLevel nserviceBusLogLevel)
    {
        var context = ScenarioContext.Current ?? scenarioContext;
        switch (logLevel)
        {
            case LogLevel.Debug:
                nserviceBusLogLevel = Logging.LogLevel.Debug;
                return context.LogLevel <= Logging.LogLevel.Debug;
            case LogLevel.Information:
                nserviceBusLogLevel = Logging.LogLevel.Info;
                return context.LogLevel <= Logging.LogLevel.Info;
            case LogLevel.Warning:
                nserviceBusLogLevel = Logging.LogLevel.Warn;
                return context.LogLevel <= Logging.LogLevel.Warn;
            case LogLevel.Error:
                nserviceBusLogLevel = Logging.LogLevel.Error;
                return context.LogLevel <= Logging.LogLevel.Error;
            case LogLevel.Critical:
                nserviceBusLogLevel = Logging.LogLevel.Fatal;
                return context.LogLevel <= Logging.LogLevel.Fatal;
            case LogLevel.Trace:
            case LogLevel.None:
            default:
                nserviceBusLogLevel = Logging.LogLevel.Debug;
                return false;
        }
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => externalScopeProvider.Push(state);
}