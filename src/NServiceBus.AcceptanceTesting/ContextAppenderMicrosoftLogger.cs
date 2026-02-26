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

        string message = exception is null ? formatter(state, exception) : $"{formatter(state, exception)} {exception}";
        if (scenarioContext.IncludeLoggingScopes)
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

        Trace.WriteLine(message);
        scenarioContext.Logs.Enqueue(new ScenarioContext.LogItem
        {
            Endpoint = ScenarioContext.CurrentEndpoint,
            LoggerName = name,
            Level = messageSeverity,
            Message = message
        });
    }

    public bool IsEnabled(LogLevel logLevel) => IsEnabled(logLevel, out _);

    bool IsEnabled(LogLevel logLevel, out NServiceBus.Logging.LogLevel nserviceBusLogLevel)
    {
        switch (logLevel)
        {
            case LogLevel.Debug:
                nserviceBusLogLevel = Logging.LogLevel.Debug;
                return scenarioContext.LogLevel <= Logging.LogLevel.Debug;
            case LogLevel.Information:
                nserviceBusLogLevel = Logging.LogLevel.Info;
                return scenarioContext.LogLevel <= Logging.LogLevel.Info;
            case LogLevel.Warning:
                nserviceBusLogLevel = Logging.LogLevel.Warn;
                return scenarioContext.LogLevel <= Logging.LogLevel.Warn;
            case LogLevel.Error:
                nserviceBusLogLevel = Logging.LogLevel.Error;
                return scenarioContext.LogLevel <= Logging.LogLevel.Error;
            case LogLevel.Trace or LogLevel.Critical or LogLevel.None:
            default:
                nserviceBusLogLevel = Logging.LogLevel.Debug; // default to Debug for unsupported levels
                return false;
        }
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => externalScopeProvider.Push(state);
}