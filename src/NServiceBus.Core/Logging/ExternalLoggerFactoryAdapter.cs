#nullable enable
namespace NServiceBus;

using System;
using Logging;
using Microsoft.Extensions.Logging;
using ILoggerFactory = Logging.ILoggerFactory;

sealed class ExternalLoggerFactoryAdapter(ILoggerFactory externalFactory, Microsoft.Extensions.Logging.ILoggerFactory microsoftLoggerFactory)
    : ILoggerFactory, ISlotScopedLoggerFactory
{
    readonly ILogger scopeLogger = microsoftLoggerFactory.CreateLogger(MicrosoftLoggerFactoryAdapter.ScopeLoggerName);

    public ILog GetLogger(Type type) => externalFactory.GetLogger(type);
    public ILog GetLogger(string name) => externalFactory.GetLogger(name);
    public IDisposable BeginScope(LogScopeState scopeState) => scopeLogger.BeginScope(scopeState) ?? NullScope.Instance;
}