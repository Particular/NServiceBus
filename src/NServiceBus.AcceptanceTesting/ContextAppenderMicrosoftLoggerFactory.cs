namespace NServiceBus.AcceptanceTesting;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

sealed class ContextAppenderMicrosoftLoggerFactory(ScenarioContext scenarioContext) : ILoggerFactory
{
    readonly ConcurrentDictionary<string, ContextAppenderMicrosoftLogger> loggers = new();
    readonly IExternalScopeProvider scopeProvider = new LoggerExternalScopeProvider();

    public void Dispose() => loggers.Clear();

    public ILogger CreateLogger(string categoryName) => loggers.GetOrAdd(categoryName, static (name, state) => new ContextAppenderMicrosoftLogger(name, state.scenarioContext, state.scopeProvider), (scenarioContext, scopeProvider));

    public void AddProvider(ILoggerProvider provider)
    {
    }
}